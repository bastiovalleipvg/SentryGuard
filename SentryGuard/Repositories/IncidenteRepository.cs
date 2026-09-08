using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Implementación del repositorio para Incidentes
    /// </summary>
    public class IncidenteRepository : Repository<Incidente>, IIncidenteRepository
    {
        private readonly SentryGuardDbContext _dbContext;

        public IncidenteRepository(SentryGuardDbContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<IEnumerable<Incidente>> GetByOrganizacionAsync(int organizacionId)
        {
            return await _dbSet
                .Where(i => i.OrganizacionId == organizacionId)
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<Incidente>> GetByAnalistaAsync(int analistaId)
        {
            return await _dbSet
                .Where(i => i.AnalistaAsignadoId == analistaId)
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<Incidente>> GetBySeveridadAsync(int severidadId)
        {
            return await _dbSet
                .Where(i => i.SeveridadId == severidadId)
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<Incidente>> GetByEstadoAsync(int estadoId)
        {
            return await _dbSet
                .Where(i => i.EstadoId == estadoId)
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Incidente?> GetWithDetallesAsync(int incidenteId)
        {
            return await _dbSet
                .Include(i => i.Severidad)
                .Include(i => i.Estado)
                .Include(i => i.Activo)
                .Include(i => i.AnalistaAsignado)
                .FirstOrDefaultAsync(i => i.Id == incidenteId);
        }
    }
}

