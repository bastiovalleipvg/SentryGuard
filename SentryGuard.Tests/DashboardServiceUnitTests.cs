using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Models;
using SentryGuard.Services;
using Xunit;

namespace SentryGuard.Tests
{
    /// <summary>
    /// Pruebas unitarias para fórmulas y validaciones de DashboardService (Hito 4 / R3).
    /// </summary>
    public class DashboardServiceUnitTests
    {
        private SentryGuardDbContext CrearDbContextInMemory()
        {
            var options = new DbContextOptionsBuilder<SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new SentryGuardDbContext(options);
        }

        private AuthService CrearAuthService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClaveSecretaSuperLargaParaPruebasUnitariasSentryGuard1234567890!",
                    ["Jwt:Issuer"] = "SentryGuardTest",
                    ["Jwt:Audience"] = "SentryGuardAudience"
                })
                .Build();
            return new AuthService(config);
        }

        [Fact]
        public async Task ObtenerIndicadores_RangoFechasInvalido_Retorna400Validation()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var authService = CrearAuthService();
            var service = new DashboardService(db, authService);

            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "Admin",
                Email = "admin@sentryguard.cl",
                Rol = new Rol { Id = 1, Nombre = "Administrador" }
            };

            var desde = new DateOnly(2026, 11, 20);
            var hasta = new DateOnly(2026, 11, 10); // Menor que desde

            // Act
            var result = await service.ObtenerIndicadoresAsync(desde, hasta, null, null, null, usuario);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("hasta"));
        }

        [Fact]
        public async Task ObtenerSeries_RangoFechasInvalido_Retorna400Validation()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var authService = CrearAuthService();
            var service = new DashboardService(db, authService);

            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "Admin",
                Email = "admin@sentryguard.cl",
                Rol = new Rol { Id = 1, Nombre = "Administrador" }
            };

            var desde = new DateOnly(2026, 12, 1);
            var hasta = new DateOnly(2026, 11, 1);

            // Act
            var result = await service.ObtenerSeriesAsync(desde, hasta, null, null, null, usuario);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("hasta"));
        }

        [Fact]
        public void IndicadoresDTO_EstructuraContratoOpenApi_PoseeCamposRequeridos()
        {
            // Arrange & Act
            var dto = new IndicadorDTO
            {
                Clave = "cumplimientoSla",
                Nombre = "Cumplimiento del SLA",
                Valor = 87.5m,
                Unidad = "porcentaje",
                Formula = "cerrados dentro del plazo / cerrados × 100",
                Variacion = 2.5m
            };

            // Assert
            Assert.Equal("cumplimientoSla", dto.Clave);
            Assert.Equal("Cumplimiento del SLA", dto.Nombre);
            Assert.Equal(87.5m, dto.Valor);
            Assert.Equal("porcentaje", dto.Unidad);
            Assert.Equal(2.5m, dto.Variacion);
        }

        [Fact]
        public void SeriesDashboardDTO_EstructuraContratoOpenApi_PoseeSeisSeriesRequeridas()
        {
            // Arrange & Act
            var series = new SeriesDashboardDTO();

            // Assert
            Assert.NotNull(series.IncidentesPorMes);
            Assert.NotNull(series.CumplimientoSlaPorMes);
            Assert.NotNull(series.PorSeveridad);
            Assert.NotNull(series.PorCategoria);
            Assert.NotNull(series.PorOrganizacion);
            Assert.NotNull(series.CargaPorAnalista);
        }
    }
}
