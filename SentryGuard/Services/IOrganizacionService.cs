using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Models;

namespace SentryGuard.Services
{
    public record OrganizacionDto(string? RazonSocial, string? Rut, int PlanId);
    public record DetalleContratoDto(int SeveridadId, decimal HorasSla);
    public record CrearContratoDto(DateTime VigenteDesde, DateTime? VigenteHasta, List<DetalleContratoDto>? Detalles);

    /// <summary>
    /// Interfaz del servicio de lógica de negocio para Organizaciones y Contratos SLA (ADR-002: Arquitectura en Capas).
    /// </summary>
    public interface IOrganizacionService
    {
        Task<ServiceResult<PagedResult<OrganizacionDTO>>> ListarAsync(int page, int pageSize, string? q, int? planId, string? orden = null);
        Task<ServiceResult<OrganizacionDTO>> ObtenerAsync(int id, Usuario usuario);
        Task<ServiceResult<OrganizacionDTO>> CrearAsync(OrganizacionDto dto);
        Task<ServiceResult<bool>> ActualizarAsync(int id, OrganizacionDto dto);
        Task<ServiceResult<bool>> EliminarAsync(int id);
        Task<ServiceResult<IEnumerable<ContratoSlaDTO>>> ListarContratosAsync(int id, Usuario usuario);
        Task<ServiceResult<int>> CrearContratoAsync(int organizacionId, CrearContratoDto dto);
        Task<ServiceResult<bool>> FinalizarContratoAsync(int organizacionId, int contratoId);
        Task<ServiceResult<bool>> EliminarContratoAsync(int organizacionId, int contratoId);
    }
}

