using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Implementación del repositorio para Usuarios
    /// </summary>
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        private readonly SentryGuardDbContext _dbContext;

        public UsuarioRepository(SentryGuardDbContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> GetWithRolAsync(int usuarioId)
        {
            return await _dbSet
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);
        }

        public async Task<IEnumerable<Usuario>> GetByOrganizacionAsync(int organizacionId)
        {
            return await _dbSet
                .Where(u => u.OrganizacionId == organizacionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Usuario>> GetAnalistasAsync()
        {
            return await _dbSet
                .Where(u => u.Rol!.Nombre == "Analista" && u.Activo)
                .ToListAsync();
        }
    }
}
