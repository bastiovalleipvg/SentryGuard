using SentryGuard.Common;
using SentryGuard.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace SentryGuard.Services
{
    /// <summary>
    /// Adaptador para el motor de Inteligencia Artificial (ADR-007 / RF-23 / RF-24).
    /// Desacopla la lógica de negocio del proveedor de modelos (Gemini, Claude, mock).
    /// </summary>
    public interface IAsistenteIA
    {
        /// <summary>
        /// Sugiere severidad, categoría y primeras acciones a partir del título y descripción (RF-23).
        /// </summary>
        Task<ServiceResult<ClasificacionIaDTO>> ClasificarIncidenteAsync(
            string titulo,
            string descripcion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Genera un resumen ejecutivo en lenguaje natural para reportes o dashboard (RF-24).
        /// </summary>
        Task<ServiceResult<ResumenIaDTO>> ResumirAsync(
            string contexto,
            string? tipo = "general",
            CancellationToken cancellationToken = default);
    }
}
