using Microsoft.AspNetCore.Http;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    /// <summary>
    /// Adaptador HTTP de Minimal APIs para Incidentes (ADR-002: Arquitectura en Capas).
    /// Los endpoints solo manejan el protocolo HTTP y delegan toda la lógica a IIncidenteService.
    /// </summary>
    public static class IncidenteAPI
    {
        public static void MapIncidenteApi(this WebApplication app)
        {
            var incidentes = app.MapGroup("/api/v1/incidentes")
                .WithTags("Incidentes")
                .RequireAuthorization();

            // Cliente crea sus propios incidentes; Administrador/Supervisor pueden crearlos por cualquier organización.
            incidentes.MapPost("", CrearIncidente).WithName("Crear incidente")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Cliente"))
                .RequireRateLimiting("incidentes-crear");

            incidentes.MapGet("", ListarIncidentes).WithName("Listar incidentes");
            incidentes.MapGet("/{id}", ObtenerIncidente).WithName("Obtener incidente");

            incidentes.MapPut("/{id}", ActualizarIncidente).WithName("Actualizar incidente")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));

            incidentes.MapDelete("/{id}", EliminarIncidente).WithName("Eliminar incidente")
                .RequireAuthorization(p => p.RequireRole("Administrador"));

            incidentes.MapPost("/{id}/asignar", AsignarAnalista).WithName("Asignar analista")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));

            incidentes.MapPost("/{id}/cambiar-estado", CambiarEstado).WithName("Cambiar estado")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));

            incidentes.MapPost("/{id}/bitacora", AgregarBitacora).WithName("Agregar bitácora")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Analista"));

            incidentes.MapGet("/{id}/bitacora", ObtenerBitacora).WithName("Obtener bitácora");

            incidentes.MapPost("/{id}/evidencia", AgregarEvidencia).WithName("Agregar evidencia")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Analista"));

            incidentes.MapGet("/{id}/evidencia", ObtenerEvidencia).WithName("Obtener evidencia");

            incidentes.MapGet("/sla/cumplimiento/{id}", ConsultarCumplimientoSla).WithName("Cumplimiento SLA");
        }

        private static async Task<IResult> CrearIncidente(
            CrearIncidenteRequest request,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var dto = new CrearIncidenteDto(request.Titulo, request.Descripcion, request.ActivoId, request.SeveridadId, request.CategoriaId);
            var result = await service.CrearIncidenteAsync(dto, usuario);
            return result.ToHttpResult(result.Success ? $"/api/v1/incidentes/{result.Data!.Id}" : null);
        }

        private static async Task<IResult> ListarIncidentes(
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService,
            int page = 1,
            int pageSize = 10,
            string? q = null,
            int? estadoId = null,
            int? severidadId = null,
            int? activoId = null,
            int? categoriaId = null,
            string? orden = null)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ListarIncidentesAsync(usuario, page, pageSize, q, estadoId, severidadId, activoId, categoriaId, orden);
            return result.ToHttpResult();
        }

        private static async Task<IResult> ObtenerIncidente(
            int id,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ObtenerIncidenteAsync(id, usuario);
            return result.ToHttpResult();
        }

        private static async Task<IResult> ActualizarIncidente(
            int id,
            ActualizarIncidenteRequest request,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var dto = new ActualizarIncidenteDto(request.Titulo, request.Descripcion, request.SeveridadId, request.CategoriaId);
            var result = await service.ActualizarIncidenteAsync(id, dto, usuario);
            return result.Success ? Results.Ok(new { mensaje = "Actualizado" }) : result.ToHttpResult();
        }

        private static async Task<IResult> EliminarIncidente(
            int id,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.EliminarIncidenteAsync(id, usuario);
            return result.Success ? Results.NoContent() : result.ToHttpResult();
        }

        private static async Task<IResult> AsignarAnalista(
            int id,
            AsignarAnalistaRequest request,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.AsignarAnalistaAsync(id, request.AnalistaId, usuario);
            return result.Success ? Results.Ok(new { mensaje = "Asignado" }) : result.ToHttpResult();
        }

        private static async Task<IResult> CambiarEstado(
            int id,
            CambiarEstadoRequest request,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.CambiarEstadoAsync(id, request.EstadoId, usuario);
            return result.Success ? Results.Ok(new { mensaje = "Estado actualizado" }) : result.ToHttpResult();
        }

        private static async Task<IResult> AgregarBitacora(
            int id,
            AgregarBitacoraRequest request,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.AgregarBitacoraAsync(id, request.Accion, request.Comentario, usuario);
            return result.ToHttpResult(result.Success ? $"/api/v1/incidentes/{id}/bitacora/{result.Data!.Id}" : null);
        }

        private static async Task<IResult> ObtenerBitacora(
            int id,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ObtenerBitacoraAsync(id, usuario);
            // Un incidente sin entradas devuelve lista vacia (200), no 404. Formato { total, items }.
            return result.Success ? Results.Ok(new { total = result.Data!.Count(), items = result.Data }) : result.ToHttpResult();
        }

        private static async Task<IResult> AgregarEvidencia(
            int id,
            AgregarEvidenciaRequest request,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.AgregarEvidenciaAsync(id, request.NombreArchivo, request.RutaAlmacenamiento, usuario);
            return result.ToHttpResult(result.Success ? $"/api/v1/incidentes/{id}/evidencia/{result.Data!.Id}" : null);
        }

        private static async Task<IResult> ObtenerEvidencia(
            int id,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ObtenerEvidenciasAsync(id, usuario);
            return result.Success ? Results.Ok(new { total = result.Data!.Count(), items = result.Data }) : result.ToHttpResult();
        }

        private static async Task<IResult> ConsultarCumplimientoSla(
            int id,
            IIncidenteService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ConsultarCumplimientoSlaAsync(id, usuario);
            return result.ToHttpResult();
        }

        record CrearIncidenteRequest(string? Titulo, string? Descripcion, int ActivoId, int SeveridadId, int? CategoriaId = null);
        record ActualizarIncidenteRequest(string? Titulo, string? Descripcion, int? SeveridadId, int? CategoriaId = null);
        record AsignarAnalistaRequest(int AnalistaId);
        record CambiarEstadoRequest(int EstadoId);
        record AgregarBitacoraRequest(string Accion, string? Comentario);
        record AgregarEvidenciaRequest(string NombreArchivo, string RutaAlmacenamiento);
    }
}
