using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Implementación del repositorio para Activos
    /// </summary>
    public class ActivoRepository : Repository<Activo>, IActivoRepository
    {
        private readonly SentryGuardDbContext _dbContext;

        public ActivoRepository(SentryGuardDbContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<IEnumerable<Activo>> GetByOrganizacionAsync(int organizacionId)
        {
            return await _dbSet
                .Where(a => a.OrganizacionId == organizacionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Activo>> GetByCategoriaAsync(int categoriaId)
        {
            return await _dbSet
                .Where(a => a.CategoriaId == categoriaId)
                .ToListAsync();
        }

        public async Task<Activo?> GetWithDetallesAsync(int activoId)
        {
            return await _dbSet
                .Include(a => a.Categoria)
                .FirstOrDefaultAsync(a => a.Id == activoId);
        }
    }
}
