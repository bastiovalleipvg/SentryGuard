using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Models;

namespace SentryGuard.Services
{
    public record CrearActivoDto(int OrganizacionId, string? Nombre, string? HostnameIp, int CategoriaId);
    public record ActualizarActivoDto(string? Nombre, string? HostnameIp, int CategoriaId);

    /// <summary>
    /// Interfaz del servicio de lógica de negocio para Activos (ADR-002: Arquitectura en Capas).
    /// </summary>
    public interface IActivoService
    {
        Task<ServiceResult<PagedResult<ActivoDTO>>> ListarAsync(Usuario usuario, int page, int pageSize, string? q, int? categoriaId, int? organizacionId, string? orden = null);
        Task<ServiceResult<ActivoDTO>> ObtenerAsync(int id, Usuario usuario);
        Task<ServiceResult<ActivoDTO>> CrearAsync(CrearActivoDto dto);
        Task<ServiceResult<bool>> ActualizarAsync(int id, ActualizarActivoDto dto);
        Task<ServiceResult<bool>> EliminarAsync(int id);
    }
}

