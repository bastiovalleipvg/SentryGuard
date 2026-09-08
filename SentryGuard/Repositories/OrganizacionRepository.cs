using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Implementación del repositorio para Organizaciones
    /// </summary>
    public class OrganizacionRepository : Repository<Organizacion>, IOrganizacionRepository
    {
        private readonly SentryGuardDbContext _dbContext;

        public OrganizacionRepository(SentryGuardDbContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<Organizacion?> GetByRutAsync(string rut)
        {
            return await _dbSet.FirstOrDefaultAsync(o => o.Rut == rut);
        }

        public async Task<Organizacion?> GetWithPlanAsync(int organizacionId)
        {
            return await _dbSet
                .Include(o => o.Plan)
                .FirstOrDefaultAsync(o => o.Id == organizacionId);
        }

        public async Task<Organizacion?> GetWithContratoSlaAsync(int organizacionId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(o => o.Id == organizacionId);
        }
    }
}

