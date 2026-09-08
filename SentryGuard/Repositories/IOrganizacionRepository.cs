using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Repositorio específico para Organizaciones
    /// </summary>
    public interface IOrganizacionRepository : IRepository<Organizacion>
    {
        Task<Organizacion?> GetByRutAsync(string rut);
        Task<Organizacion?> GetWithPlanAsync(int organizacionId);
        Task<Organizacion?> GetWithContratoSlaAsync(int organizacionId);
    }
}
