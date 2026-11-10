using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Models;

namespace SentryGuard.Services
{
    public record CrearIncidenteDto(string? Titulo, string? Descripcion, int ActivoId, int SeveridadId);
    public record ActualizarIncidenteDto(string? Titulo, string? Descripcion, int? SeveridadId);

    /// <summary>
    /// Interfaz del servicio de lógica de negocio para Incidentes (ADR-002: Arquitectura en Capas).
    /// </summary>
    public interface IIncidenteService
    {
        Task<ServiceResult<IncidenteDTO>> CrearIncidenteAsync(CrearIncidenteDto dto, Usuario usuario);
        Task<ServiceResult<IncidenteDTO>> ObtenerIncidenteAsync(int id, Usuario usuario);
        Task<ServiceResult<PagedResult<IncidenteDTO>>> ListarIncidentesAsync(Usuario usuario, int page, int pageSize, string? q, int? estadoId, int? severidadId, int? activoId);
        Task<ServiceResult<bool>> ActualizarIncidenteAsync(int id, ActualizarIncidenteDto dto, Usuario usuario);
        Task<ServiceResult<bool>> EliminarIncidenteAsync(int id, Usuario usuario);
        Task<ServiceResult<bool>> AsignarAnalistaAsync(int id, int analistaId, Usuario usuario);
        Task<ServiceResult<bool>> CambiarEstadoAsync(int id, int nuevoEstadoId, Usuario usuario);
        Task<ServiceResult<BitacoraDTO>> AgregarBitacoraAsync(int id, string accion, string? comentario, Usuario usuario);
        Task<ServiceResult<IEnumerable<BitacoraDTO>>> ObtenerBitacoraAsync(int id, Usuario usuario);
        Task<ServiceResult<EvidenciaDTO>> AgregarEvidenciaAsync(int id, string nombreArchivo, string rutaAlmacenamiento, Usuario usuario);
        Task<ServiceResult<IEnumerable<EvidenciaDTO>>> ObtenerEvidenciasAsync(int id, Usuario usuario);
        Task<ServiceResult<CumplimientoSLADTO>> ConsultarCumplimientoSlaAsync(int id, Usuario usuario);
    }
}

