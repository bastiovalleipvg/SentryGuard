using System;
using System.Threading;
using System.Threading.Tasks;
using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Models;

namespace SentryGuard.Services
{
    public interface IDashboardService
    {
        Task<ServiceResult<IndicadoresDTO>> ObtenerIndicadoresAsync(
            DateOnly? desde,
            DateOnly? hasta,
            int? organizacionId,
            int? severidadId,
            int? categoriaId,
            Usuario usuario,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<SeriesDashboardDTO>> ObtenerSeriesAsync(
            DateOnly? desde,
            DateOnly? hasta,
            int? organizacionId,
            int? severidadId,
            int? categoriaId,
            Usuario usuario,
            CancellationToken cancellationToken = default);
    }
}
