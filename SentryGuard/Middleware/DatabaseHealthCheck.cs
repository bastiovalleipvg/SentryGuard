using Microsoft.Extensions.Diagnostics.HealthChecks;
using SentryGuard.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SentryGuard.Middleware
{
    /// <summary>
    /// Health Check para verificar la disponibilidad de la base de datos (RNF-04).
    /// Usado por Docker compose y herramientas de monitoreo en /health.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly SentryGuardDbContext _db;

        public DatabaseHealthCheck(SentryGuardDbContext db)
        {
            _db = db;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
                if (canConnect)
                {
                    return HealthCheckResult.Healthy("Conexión con la base de datos operativa.");
                }

                return HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Fallo al verificar disponibilidad de la base de datos.", ex);
            }
        }
    }
}
