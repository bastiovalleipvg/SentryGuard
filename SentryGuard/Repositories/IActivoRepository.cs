using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Repositorio específico para Activos
    /// </summary>
    public interface IActivoRepository : IRepository<Activo>
    {
        Task<IEnumerable<Activo>> GetByOrganizacionAsync(int organizacionId);
        Task<IEnumerable<Activo>> GetByCategoriaAsync(int categoriaId);
        Task<Activo?> GetWithDetallesAsync(int activoId);
    }
}
