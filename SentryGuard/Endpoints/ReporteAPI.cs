using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Services;
using SentryGuard.Services.Reportes;

namespace SentryGuard.Endpoints
{
    public static class ReporteAPI
    {
        public static void MapReporteApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/reportes")
                .WithTags("Reportes")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Cliente"));

            grupo.MapGet("/{tipo}", ObtenerReporte)
                .WithName("ObtenerReporte")
                .WithSummary("Datos paginados de un reporte (operativo, gerencial o sla)")
                .Produces<PaginaReporteDTO>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status403Forbidden);

            grupo.MapGet("/{tipo}/exportar", Exportar)
                .WithName("ExportarReporte")
                .WithSummary("Exporta el reporte a xlsx, pdf o csv con los mismos filtros (queda en la auditoría)")
                .RequireRateLimiting("exportaciones")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status429TooManyRequests);
        }

        private static async Task<IResult> ObtenerReporte(
            string tipo, DateOnly? desde, DateOnly? hasta, int? organizacionId, int? severidadId, int? estadoId, int? categoriaId,
            string? orden,
            IReporteService reportes, HttpContext httpContext, SentryGuardDbContext db, AuthService authService, CancellationToken ct,
            int page = 1, int pageSize = 10)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();
            var (p, ps) = Paginacion.Normalizar(page, pageSize);
            var filtros = new FiltrosReporte(desde, hasta, organizacionId, severidadId, estadoId, categoriaId);
            return (await reportes.ObtenerPaginaAsync(tipo, filtros, p, ps, orden, usuario, ct)).ToHttpResult();
        }

        private static async Task<IResult> Exportar(
            string tipo, string? formato, DateOnly? desde, DateOnly? hasta, int? organizacionId, int? severidadId, int? estadoId, int? categoriaId,
            IReporteService reportes, IEnumerable<IExportador> exportadores,
            HttpContext httpContext, SentryGuardDbContext db, AuthService authService, CancellationToken ct)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var exportador = exportadores.FirstOrDefault(e => e.Formato.Equals(formato, StringComparison.OrdinalIgnoreCase));
            if (exportador == null)
                return ApiResults.Validacion(new System.Collections.Generic.Dictionary<string, string[]>
                {
                    ["formato"] = ["Formato no válido. Usa xlsx, pdf o csv."]
                });

            var filtros = new FiltrosReporte(desde, hasta, organizacionId, severidadId, estadoId, categoriaId);
            var resultado = await reportes.ObtenerContenidoAsync(tipo, filtros, usuario, ct);
            if (!resultado.Success) return resultado.ToHttpResult();

            await reportes.RegistrarExportacionAsync(tipo, exportador.Formato, filtros, usuario, ct);

            using var buffer = new MemoryStream();
            exportador.Exportar(resultado.Data!, buffer);
            var nombre = $"reporte-{tipo}-{DateTime.Now:yyyy-MM-dd}.{exportador.Extension}";
            return Results.File(buffer.ToArray(), exportador.ContentType, nombre);
        }
    }
}
