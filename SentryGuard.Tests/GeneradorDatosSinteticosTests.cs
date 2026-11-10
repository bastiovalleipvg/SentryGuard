using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SentryGuard.Data;
using SentryGuard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SentryGuard.Tests
{
    /// <summary>
    /// Pruebas unitarias para el generador de datos sintéticos (ADR-009 / RF-25).
    /// Valida que genere al menos 500 incidentes coherentes distribuidos en 12 meses
    /// y que su ejecución sea estrictamente idempotente.
    /// </summary>
    public class GeneradorDatosSinteticosTests
    {
        private SentryGuardDbContext CrearContextoEnMemoria()
        {
            var options = new DbContextOptionsBuilder<SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var db = new SentryGuardDbContext(options);

            // Seed mínimo de catálogos necesarios para las relaciones FK
            db.Roles.AddRange(
                new Rol { Id = 1, Nombre = "Administrador" },
                new Rol { Id = 2, Nombre = "Supervisor" },
                new Rol { Id = 3, Nombre = "Analista" },
                new Rol { Id = 4, Nombre = "Cliente" }
            );

            db.PlanesServicio.AddRange(
                new PlanServicio { Id = 1, Nombre = "Básico", TiempoRespuestaBaseH = 24m },
                new PlanServicio { Id = 2, Nombre = "Estándar", TiempoRespuestaBaseH = 12m },
                new PlanServicio { Id = 3, Nombre = "Premium", TiempoRespuestaBaseH = 4m }
            );

            db.CategoriasActivo.AddRange(
                new CategoriaActivo { Id = 1, Nombre = "Servidor" },
                new CategoriaActivo { Id = 2, Nombre = "Endpoint" },
                new CategoriaActivo { Id = 3, Nombre = "Red" },
                new CategoriaActivo { Id = 4, Nombre = "Aplicación" },
                new CategoriaActivo { Id = 5, Nombre = "Nube" }
            );

            db.Severidades.AddRange(
                new Severidad { Id = 1, Nombre = "Baja", HorasSla = 24m },
                new Severidad { Id = 2, Nombre = "Media", HorasSla = 12m },
                new Severidad { Id = 3, Nombre = "Alta", HorasSla = 4m },
                new Severidad { Id = 4, Nombre = "Crítica", HorasSla = 1m }
            );

            db.EstadosIncidente.AddRange(
                new EstadoIncidente { Id = 1, Nombre = "Abierto" },
                new EstadoIncidente { Id = 2, Nombre = "En progreso" },
                new EstadoIncidente { Id = 3, Nombre = "Escalado" },
                new EstadoIncidente { Id = 4, Nombre = "Resuelto" },
                new EstadoIncidente { Id = 5, Nombre = "Cerrado" }
            );

            db.Organizaciones.Add(new Organizacion
            {
                Id = 1,
                RazonSocial = "Empresa Demo SpA",
                Rut = "76123456-7",
                PlanContratadoId = 2,
                FechaAlta = DateTime.UtcNow.AddYears(-1)
            });

            db.Activos.AddRange(
                new Activo { Id = 1, OrganizacionId = 1, Nombre = "Servidor Web", HostnameIp = "10.10.0.10", CategoriaId = 1 },
                new Activo { Id = 2, OrganizacionId = 1, Nombre = "Firewall Perimetral", HostnameIp = "10.10.0.1", CategoriaId = 3 }
            );

            db.Usuarios.AddRange(
                new Usuario { Id = 1, Nombre = "Admin", Email = "admin@sentryguard.cl", PasswordHash = "hash", RolId = 1, Activo = true },
                new Usuario { Id = 3, Nombre = "Analista Demo", Email = "analista@sentryguard.cl", PasswordHash = "hash", RolId = 3, Activo = true }
            );

            db.SaveChanges();
            return db;
        }

        [Fact]
        public async Task Generador_CreaAlMenos500IncidentesConBitacoraYEvidencias()
        {
            // Arrange
            using var db = CrearContextoEnMemoria();
            var logger = NullLogger<GeneradorDatosSinteticos>.Instance;
            var generador = new GeneradorDatosSinteticos(db, logger);

            // Act
            var totalGenerados = await generador.GenerarAsync();

            // Assert
            Assert.True(totalGenerados >= 500, $"Se esperaban al menos 500 incidentes pero se obtuvieron {totalGenerados}");

            var totalIncidentesEnBd = await db.Incidentes.CountAsync();
            Assert.Equal(totalGenerados, totalIncidentesEnBd);

            // Verificar que existen bitácoras creadas
            var totalBitacoras = await db.BitacorasIncidente.CountAsync();
            Assert.True(totalBitacoras >= totalGenerados, "Debe existir al menos una entrada de bitácora por cada incidente");

            // Verificar que existen evidencias
            var totalEvidencias = await db.EvidenciasIncidente.CountAsync();
            Assert.True(totalEvidencias > 0, "Debe haber evidencias registradas para una porción de incidentes");

            // Verificar coherencia temporal: fechas distribuidas en los últimos 12 meses
            var fechaMin = await db.Incidentes.MinAsync(i => i.FechaCreacion);
            var fechaMax = await db.Incidentes.MaxAsync(i => i.FechaCreacion);
            var rangoDias = (fechaMax - fechaMin).TotalDays;
            Assert.True(rangoDias > 300, $"El rango histórico ({rangoDias:N0} días) debe abarcar al menos 300 días (~12 meses)");

            // Verificar que los incidentes cerrados/resueltos tienen FechaCierre >= FechaCreacion
            var cerrados = await db.Incidentes.Where(i => i.EstadoId == 4 || i.EstadoId == 5).ToListAsync();
            Assert.All(cerrados, inc =>
            {
                Assert.NotNull(inc.FechaCierre);
                Assert.True(inc.FechaCierre >= inc.FechaCreacion);
            });
        }

        [Fact]
        public async Task Generador_EsIdempotente_NoDuplicaDatosSiYaExisten500OMas()
        {
            // Arrange
            using var db = CrearContextoEnMemoria();
            var logger = NullLogger<GeneradorDatosSinteticos>.Instance;
            var generador = new GeneradorDatosSinteticos(db, logger);

            // Primera ejecución
            var totalPrimeraVez = await generador.GenerarAsync();
            Assert.True(totalPrimeraVez >= 500);

            // Act: Segunda ejecución
            var totalSegundaVez = await generador.GenerarAsync();

            // Assert: El conteo total no debe cambiar
            Assert.Equal(totalPrimeraVez, totalSegundaVez);
            var conteoFinal = await db.Incidentes.CountAsync();
            Assert.Equal(totalPrimeraVez, conteoFinal);
        }
    }
}

