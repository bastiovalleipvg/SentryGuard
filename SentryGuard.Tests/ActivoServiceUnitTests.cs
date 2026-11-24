using Microsoft.EntityFrameworkCore;
using Moq;
using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Models;
using SentryGuard.Repositories;
using SentryGuard.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SentryGuard.Tests
{
    /// <summary>
    /// Pruebas unitarias para ActivoService (ADR-002: Lógica de negocio y mitigación OWASP API1).
    /// </summary>
    public class ActivoServiceUnitTests
    {
        private readonly Mock<IActivoRepository> _activoRepoMock = new();

        private Data.SentryGuardDbContext CrearDbContextInMemory()
        {
            var options = new DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new Data.SentryGuardDbContext(options);
        }

        [Fact]
        public async Task ListarActivos_Cliente_SoloVeActivosDeSuOrganizacion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.Organizaciones.Add(new Organizacion { Id = 1, RazonSocial = "Org 1", Rut = "76123456-7" });
            db.Organizaciones.Add(new Organizacion { Id = 2, RazonSocial = "Org 2", Rut = "76987654-3" });
            db.CategoriasActivo.Add(new CategoriaActivo { Id = 1, Nombre = "Servidor" });

            db.Activos.AddRange(
                new Activo { Id = 1, OrganizacionId = 1, Nombre = "Servidor Org 1", CategoriaId = 1 },
                new Activo { Id = 2, OrganizacionId = 2, Nombre = "Servidor Org 2", CategoriaId = 1 }
            );
            await db.SaveChangesAsync();

            var service = new ActivoService(_activoRepoMock.Object, db);
            var usuarioCliente = new Usuario { Id = 10, OrganizacionId = 1, Rol = new Rol { Nombre = "Cliente" } };

            // Act
            var result = await service.ListarAsync(usuarioCliente, 1, 10, null, null, null);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            var item = System.Linq.Enumerable.First(result.Data.Items);
            Assert.Equal(1, item.OrganizacionId);
            Assert.Equal("Servidor Org 1", item.Nombre);
        }

        [Fact]
        public async Task ObtenerActivo_ClienteConActivoDeOtraOrganizacion_DevuelveForbid()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.Activos.Add(new Activo { Id = 1, OrganizacionId = 2, Nombre = "Servidor Secreto", CategoriaId = 1 });
            await db.SaveChangesAsync();

            var service = new ActivoService(_activoRepoMock.Object, db);
            var usuarioCliente = new Usuario { Id = 10, OrganizacionId = 1, Rol = new Rol { Nombre = "Cliente" } };

            // Act
            var result = await service.ObtenerAsync(1, usuarioCliente);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task ObtenerActivo_Inexistente_DevuelveNotFound()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var service = new ActivoService(_activoRepoMock.Object, db);
            var usuarioAdmin = new Usuario { Id = 1, Rol = new Rol { Nombre = "Administrador" } };

            // Act
            var result = await service.ObtenerAsync(999, usuarioAdmin);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task CrearActivo_NombreCorto_DevuelveErrorValidacion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var service = new ActivoService(_activoRepoMock.Object, db);
            var dto = new CrearActivoDto(OrganizacionId: 1, Nombre: "ab", HostnameIp: "10.0.0.1", CategoriaId: 1);

            // Act
            var result = await service.CrearAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("nombre"));
        }

        [Fact]
        public async Task CrearActivo_HostnameOIpInvalida_DevuelveErrorValidacion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.CategoriasActivo.Add(new CategoriaActivo { Id = 1, Nombre = "Servidor" });
            db.Organizaciones.Add(new Organizacion { Id = 1, RazonSocial = "Org 1", Rut = "76123456-7" });
            await db.SaveChangesAsync();

            var service = new ActivoService(_activoRepoMock.Object, db);
            var dto = new CrearActivoDto(OrganizacionId: 1, Nombre: "Servidor Web", HostnameIp: "invalid ip/$$$", CategoriaId: 1);

            // Act
            var result = await service.CrearAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("hostnameIp"));
        }

        [Fact]
        public async Task CrearActivo_OrganizacionInexistente_DevuelveErrorValidacion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.CategoriasActivo.Add(new CategoriaActivo { Id = 1, Nombre = "Servidor" });
            await db.SaveChangesAsync();

            var service = new ActivoService(_activoRepoMock.Object, db);
            var dto = new CrearActivoDto(OrganizacionId: 999, Nombre: "Firewall Cisco", HostnameIp: "10.0.0.1", CategoriaId: 1);

            // Act
            var result = await service.CrearAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("organizacionId"));
        }

        [Fact]
        public async Task EliminarActivo_ConIncidentesAsociados_DevuelveConflict409()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var activo = new Activo { Id = 5, OrganizacionId = 1, Nombre = "Base de datos", CategoriaId = 1 };
            _activoRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(activo);

            db.Incidentes.Add(new Incidente
            {
                Id = 1,
                ActivoId = 5,
                OrganizacionId = 1,
                Titulo = "Alerta de inyección SQL",
                Descripcion = "Detección en base de datos",
                SeveridadId = 3,
                EstadoId = 1
            });
            await db.SaveChangesAsync();

            var service = new ActivoService(_activoRepoMock.Object, db);

            // Act
            var result = await service.EliminarAsync(5);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("ACTIVO_CON_INCIDENTES", result.Codigo);
        }

        [Fact]
        public async Task EliminarActivo_SinIncidentes_EliminaCorrectamente()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var activo = new Activo { Id = 6, OrganizacionId = 1, Nombre = "Switch Secundario", CategoriaId = 3 };
            _activoRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(activo);
            _activoRepoMock.Setup(r => r.DeleteAsync(6)).Returns(Task.CompletedTask);

            var service = new ActivoService(_activoRepoMock.Object, db);

            // Act
            var result = await service.EliminarAsync(6);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
            _activoRepoMock.Verify(r => r.DeleteAsync(6), Times.Once);
        }

        [Fact]
        public async Task ActualizarActivo_Inexistente_DevuelveNotFound()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            _activoRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Activo?)null);

            var service = new ActivoService(_activoRepoMock.Object, db);
            var dto = new ActualizarActivoDto("Nuevo Nombre", "10.0.0.5", 1);

            // Act
            var result = await service.ActualizarAsync(99, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }
    }
}
