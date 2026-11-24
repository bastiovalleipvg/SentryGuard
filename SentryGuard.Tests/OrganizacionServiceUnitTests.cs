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
    /// Pruebas unitarias para OrganizacionService (Validación de RUT chileno con DV, Contratos SLA y control de acceso).
    /// </summary>
    public class OrganizacionServiceUnitTests
    {
        private readonly Mock<IOrganizacionRepository> _orgRepoMock = new();
        private readonly AuthService _authService;

        public OrganizacionServiceUnitTests()
        {
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _authService = new AuthService(configMock.Object);
        }

        private Data.SentryGuardDbContext CrearDbContextInMemory()
        {
            var options = new DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new Data.SentryGuardDbContext(options);
        }

        [Fact]
        public async Task CrearOrganizacion_RutConDvInvalido_DevuelveErrorValidacion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.PlanesServicio.Add(new PlanServicio { Id = 1, Nombre = "Básico", TiempoRespuestaBaseH = 24m });
            await db.SaveChangesAsync();

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);
            // 76123456-9 tiene DV incorrecto (el correcto para 76123456 es 7)
            var dto = new OrganizacionDto("Empresa Test SpA", "76123456-9", 1);

            // Act
            var result = await service.CrearAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("rut"));
            Assert.Contains("dígito verificador", result.Errors["rut"][0]);
        }

        [Fact]
        public async Task CrearOrganizacion_RutValido_CreaCorrectamente()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.PlanesServicio.Add(new PlanServicio { Id = 1, Nombre = "Estándar", TiempoRespuestaBaseH = 12m });
            await db.SaveChangesAsync();

            _orgRepoMock.Setup(r => r.AddAsync(It.IsAny<Organizacion>()))
                .Callback<Organizacion>(o =>
                {
                    if (o.Id == 0) o.Id = 10;
                    db.Organizaciones.Add(o);
                    db.SaveChanges();
                })
                .ReturnsAsync((Organizacion o) => o);

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);
            var dto = new OrganizacionDto("Empresa Válida SpA", "76123456-0", 1);

            // Act
            var result = await service.CrearAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("76123456-0", result.Data.Rut);
            Assert.Equal("Empresa Válida SpA", result.Data.RazonSocial);
        }

        [Fact]
        public async Task CrearOrganizacion_RutDuplicado_DevuelveErrorValidacion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.PlanesServicio.Add(new PlanServicio { Id = 1, Nombre = "Básico", TiempoRespuestaBaseH = 24m });
            db.Organizaciones.Add(new Organizacion { Id = 1, RazonSocial = "Empresa Existente", Rut = "76123456-0", PlanContratadoId = 1 });
            await db.SaveChangesAsync();

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);
            var dto = new OrganizacionDto("Otra Empresa con Mismo RUT", "76123456-0", 1);

            // Act
            var result = await service.CrearAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("rut"));
            Assert.Contains("Ya existe una organización con ese RUT", result.Errors["rut"][0]);
        }

        [Fact]
        public async Task CrearOrganizacion_RazonSocialCorta_DevuelveErrorValidacion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.PlanesServicio.Add(new PlanServicio { Id = 1, Nombre = "Básico", TiempoRespuestaBaseH = 24m });
            await db.SaveChangesAsync();

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);
            var dto = new OrganizacionDto("AB", "76123456-7", 1);

            // Act
            var result = await service.CrearAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("razonSocial"));
        }

        [Fact]
        public async Task ObtenerOrganizacion_ClienteConOtraOrganizacion_DevuelveForbid()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.PlanesServicio.Add(new PlanServicio { Id = 1, Nombre = "Básico", TiempoRespuestaBaseH = 24m });
            db.Organizaciones.Add(new Organizacion { Id = 1, RazonSocial = "Org 1", Rut = "76123456-7", PlanContratadoId = 1 });
            db.Organizaciones.Add(new Organizacion { Id = 2, RazonSocial = "Org 2", Rut = "76987654-3", PlanContratadoId = 1 });
            await db.SaveChangesAsync();

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);
            var usuarioCliente = new Usuario { Id = 10, OrganizacionId = 1, Rol = new Rol { Nombre = "Cliente" } };

            // Act
            var result = await service.ObtenerAsync(2, usuarioCliente);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task ObtenerOrganizacion_ClienteConSuOrganizacion_DevuelveOk()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.PlanesServicio.Add(new PlanServicio { Id = 1, Nombre = "Básico", TiempoRespuestaBaseH = 24m });
            db.Organizaciones.Add(new Organizacion { Id = 1, RazonSocial = "Org Propia", Rut = "76123456-7", PlanContratadoId = 1 });
            await db.SaveChangesAsync();

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);
            var usuarioCliente = new Usuario { Id = 10, OrganizacionId = 1, Rol = new Rol { Nombre = "Cliente" } };

            // Act
            var result = await service.ObtenerAsync(1, usuarioCliente);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Org Propia", result.Data.RazonSocial);
        }

        [Fact]
        public async Task CrearContratoSla_ConContratoVigenteExistente_DevuelveErrorSuperposicion()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            db.Organizaciones.Add(new Organizacion { Id = 1, RazonSocial = "Org 1", Rut = "76123456-0" });
            db.ContratosSla.Add(new ContratoSLA { Id = 1, OrganizacionId = 1, VigenteDesde = DateTime.UtcNow.AddDays(-30), VigenteHasta = null });
            await db.SaveChangesAsync();

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);
            var detalles = new List<DetalleContratoDto> { new(1, 10m) };
            var dto = new CrearContratoDto(DateTime.UtcNow, null, detalles);

            // Act
            var result = await service.CrearContratoAsync(1, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("vigenteDesde"));
        }

        [Fact]
        public async Task FinalizarContratoSla_Existente_EstableceVigenteHasta()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var contrato = new ContratoSLA { Id = 10, OrganizacionId = 1, VigenteDesde = DateTime.UtcNow.AddMonths(-2), VigenteHasta = null };
            db.ContratosSla.Add(contrato);
            await db.SaveChangesAsync();

            var service = new OrganizacionService(_orgRepoMock.Object, _authService, db);

            // Act
            var result = await service.FinalizarContratoAsync(1, 10);

            // Assert
            Assert.True(result.Success);
            var contratoActualizado = await db.ContratosSla.FindAsync(10);
            Assert.NotNull(contratoActualizado!.VigenteHasta);
        }
    }
}
