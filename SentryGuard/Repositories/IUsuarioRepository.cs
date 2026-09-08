using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Repositorio específico para Usuarios
    /// </summary>
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetWithRolAsync(int usuarioId);
        Task<IEnumerable<Usuario>> GetByOrganizacionAsync(int organizacionId);
        Task<IEnumerable<Usuario>> GetAnalistasAsync();
    }
}
