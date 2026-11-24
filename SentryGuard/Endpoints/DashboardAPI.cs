using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    public static class DashboardAPI
    {
        public static void MapDashboardApi(this WebApplication app)
        {
            var dashboard = app.MapGroup("/api/v1/dashboard")
                .WithTags("Dashboard")
                .RequireAuthorization();

            dashboard.MapGet("/indicadores", ObtenerIndicadores)
                .WithName("ObtenerIndicadoresDashboard")
                .WithSummary("Indicadores del período filtrado (respeta la visibilidad del rol)")
                .Produces<IndicadoresDTO>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            dashboard.MapGet("/series", ObtenerSeries)
                .WithName("ObtenerSeriesDashboard")
                .WithSummary("Series para los gráficos (por mes, por severidad, por categoría, por organización y carga por analista)")
                .Produces<SeriesDashboardDTO>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized);
        }

        private static async Task<IResult> ObtenerIndicadores(
            DateOnly? desde,
            DateOnly? hasta,
            int? organizacionId,
            int? severidadId,
            int? categoriaId,
            IDashboardService dashboardService,
            HttpContext httpContext,
            SentryGuardDbContext db,
            AuthService authService,
            CancellationToken ct)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await dashboardService.ObtenerIndicadoresAsync(
                desde, hasta, organizacionId, severidadId, categoriaId, usuario, ct);

            return result.ToHttpResult();
        }

        private static async Task<IResult> ObtenerSeries(
            DateOnly? desde,
            DateOnly? hasta,
            int? organizacionId,
            int? severidadId,
            int? categoriaId,
            IDashboardService dashboardService,
            HttpContext httpContext,
            SentryGuardDbContext db,
            AuthService authService,
            CancellationToken ct)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await dashboardService.ObtenerSeriesAsync(
                desde, hasta, organizacionId, severidadId, categoriaId, usuario, ct);

            return result.ToHttpResult();
        }
    }
}
