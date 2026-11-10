using Microsoft.EntityFrameworkCore;
using Moq;
using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Models;
using SentryGuard.Repositories;
using SentryGuard.Services;
using System.Security.Claims;
using Xunit;

namespace SentryGuard.Tests
{
    /// <summary>
    /// Pruebas unitarias de la capa de servicios (ADR-002: Arquitectura en Capas).
    /// Valida que la lógica de negocio y las mitigaciones OWASP se apliquen correctamente
    /// sin depender de la base de datos ni de llamadas HTTP.
    /// </summary>
    public class IncidenteServiceUnitTests
    {
        private readonly Mock<IIncidenteRepository> _incidenteRepoMock = new();
        private readonly Mock<IActivoRepository> _activoRepoMock = new();
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();

        [Fact]
        public async Task CrearIncidente_TituloODescripcionVacia_DevuelveErrorValidacion()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            var options = new DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var db = new Data.SentryGuardDbContext(options);

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                db
            );

            var usuario = new Usuario { Id = 1, Nombre = "Test User", Rol = new Rol { Nombre = "Cliente" } };
            var dto = new CrearIncidenteDto("", "", 1, 1);

            // Act
            var result = await service.CrearIncidenteAsync(dto, usuario);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("titulo"));
            Assert.True(result.Errors.ContainsKey("descripcion"));
        }

        [Fact]
        public async Task CrearIncidente_ClienteConActivoDeOtraOrganizacion_DevuelveForbid()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            // Activo pertenece a la organizacion 2
            _activoRepoMock.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new Activo { Id = 10, Nombre = "Servidor Web", OrganizacionId = 2 });

            // Configuramos un DbContext con severidades en memoria o mock
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var db = new Data.SentryGuardDbContext(options);
            db.Severidades.Add(new Severidad { Id = 1, Nombre = "Baja", HorasSla = 48 });
            await db.SaveChangesAsync();

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                db
            );

            // Usuario Cliente pertenece a la organizacion 1
            var usuarioCliente = new Usuario
            {
                Id = 5,
                Nombre = "Cliente Org 1",
                OrganizacionId = 1,
                Rol = new Rol { Id = 4, Nombre = "Cliente" }
            };

            var dto = new CrearIncidenteDto("Intento de ataque", "Descripcion del incidente detectado", 10, 1);

            // Act: cliente intenta crear un incidente para un activo de otra organización (OWASP API1)
            var result = await service.CrearIncidenteAsync(dto, usuarioCliente);

            // Assert: debe ser denegado
            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("SIN_PERMISO", result.Codigo);
        }

        [Fact]
        public async Task AsignarAnalista_CuandoUsuarioNoExisteONoTieneRolAnalista_DevuelveBadRequest()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            _incidenteRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Incidente { Id = 1, Titulo = "Incidente de prueba", OrganizacionId = 1 });

            // El usuario existe pero tiene rol Cliente, no Analista
            _usuarioRepoMock.Setup(r => r.GetWithRolAsync(99))
                .ReturnsAsync(new Usuario { Id = 99, Nombre = "Usuario No Analista", Activo = true, Rol = new Rol { Nombre = "Cliente" } });

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                null!
            );

            var admin = new Usuario { Id = 1, Nombre = "Admin", Rol = new Rol { Nombre = "Administrador" } };

            // Act
            var result = await service.AsignarAnalistaAsync(1, 99, admin);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("ANALISTA_INVALIDO", result.Codigo);
        }

        [Fact]
        public async Task AsignarAnalista_IncidenteNoExiste_DevuelveNotFound()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            _incidenteRepoMock.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Incidente?)null);

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                null!
            );

            var admin = new Usuario { Id = 1, Nombre = "Admin", Rol = new Rol { Nombre = "Administrador" } };

            // Act
            var result = await service.AsignarAnalistaAsync(999, 2, admin);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("RECURSO_NO_ENCONTRADO", result.Codigo);
        }

        [Fact]
        public async Task EliminarIncidente_UsuarioNoAdministrador_DevuelveForbid()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                null!
            );

            var supervisor = new Usuario { Id = 2, Nombre = "Supervisor", Rol = new Rol { Nombre = "Supervisor" } };

            // Act
            var result = await service.EliminarIncidenteAsync(1, supervisor);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("SIN_PERMISO", result.Codigo);
        }

        [Fact]
        public async Task CambiarEstado_MismoEstado_DevuelveConflicto409()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            var options = new DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var db = new Data.SentryGuardDbContext(options);
            db.EstadosIncidente.Add(new EstadoIncidente { Id = 1, Nombre = "Abierto" });
            await db.SaveChangesAsync();

            _incidenteRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Incidente { Id = 1, Titulo = "Incidente", EstadoId = 1, OrganizacionId = 1 });

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                db
            );

            var admin = new Usuario { Id = 1, Nombre = "Admin", Rol = new Rol { Nombre = "Administrador" } };

            // Act
            var result = await service.CambiarEstadoAsync(1, 1, admin);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("ESTADO_IDENTICO", result.Codigo);
        }

        [Fact]
        public async Task CambiarEstado_TransicionInvalida_DevuelveTransicionInvalida409()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            var options = new DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var db = new Data.SentryGuardDbContext(options);
            db.EstadosIncidente.AddRange(
                new EstadoIncidente { Id = 5, Nombre = "Cerrado" },
                new EstadoIncidente { Id = 3, Nombre = "Escalado" }
            );
            await db.SaveChangesAsync();

            // Incidente cerrado (EstadoId = 5) intentando saltar directo a Escalado (EstadoId = 3)
            _incidenteRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Incidente { Id = 1, Titulo = "Incidente", EstadoId = 5, OrganizacionId = 1 });

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                db
            );

            var admin = new Usuario { Id = 1, Nombre = "Admin", Rol = new Rol { Nombre = "Administrador" } };

            // Act
            var result = await service.CambiarEstadoAsync(1, 3, admin);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("TRANSICION_INVALIDA", result.Codigo);
        }

        [Fact]
        public async Task CambiarEstado_TransicionValida_ActualizaEstadoYRegistraCierre()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authService = new AuthService(configMock.Object);

            var options = new DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var db = new Data.SentryGuardDbContext(options);
            db.EstadosIncidente.AddRange(
                new EstadoIncidente { Id = 2, Nombre = "En progreso" },
                new EstadoIncidente { Id = 4, Nombre = "Resuelto" }
            );
            await db.SaveChangesAsync();

            var incidente = new Incidente { Id = 1, Titulo = "Incidente", EstadoId = 2, OrganizacionId = 1 };
            _incidenteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(incidente);
            _incidenteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Incidente>())).ReturnsAsync(incidente);

            var service = new IncidenteService(
                _incidenteRepoMock.Object,
                _activoRepoMock.Object,
                _usuarioRepoMock.Object,
                authService,
                db
            );

            var admin = new Usuario { Id = 1, Nombre = "Admin", Rol = new Rol { Nombre = "Administrador" } };

            // Act: transición válida 2 (En progreso) -> 4 (Resuelto)
            var result = await service.CambiarEstadoAsync(1, 4, admin);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(4, incidente.EstadoId);
            Assert.NotNull(incidente.FechaCierre);
            _incidenteRepoMock.Verify(r => r.UpdateAsync(incidente), Times.Once);
        }
    }
}
