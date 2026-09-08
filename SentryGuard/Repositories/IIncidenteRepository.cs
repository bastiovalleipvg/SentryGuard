using SentryGuard.Models;

namespace SentryGuard.Repositories
{
    /// <summary>
    /// Repositorio específico para Incidentes
    /// </summary>
    public interface IIncidenteRepository : IRepository<Incidente>
    {
        Task<IEnumerable<Incidente>> GetByOrganizacionAsync(int organizacionId);
        Task<IEnumerable<Incidente>> GetByAnalistaAsync(int analistaId);
        Task<IEnumerable<Incidente>> GetBySeveridadAsync(int severidadId);
        Task<IEnumerable<Incidente>> GetByEstadoAsync(int estadoId);
        Task<Incidente?> GetWithDetallesAsync(int incidenteId);
    }
}
