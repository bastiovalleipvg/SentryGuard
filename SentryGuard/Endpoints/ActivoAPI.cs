using Microsoft.AspNetCore.Http;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    /// <summary>
    /// Adaptador HTTP de Minimal APIs para Activos (ADR-002: Arquitectura en Capas).
    /// </summary>
    public static class ActivoAPI
    {
        public static void MapActivoApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/activos").WithTags("Activos").RequireAuthorization();

            grupo.MapGet("", Listar).WithName("Listar activos");
            grupo.MapGet("/{id}", Obtener).WithName("Obtener activo");

            grupo.MapPost("", Crear).WithName("Crear activo")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            grupo.MapPut("/{id}", Actualizar).WithName("Actualizar activo")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            grupo.MapDelete("/{id}", Eliminar).WithName("Eliminar activo")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static async Task<IResult> Listar(
            IActivoService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService,
            int page = 1,
            int pageSize = 10,
            string? q = null,
            int? categoriaId = null,
            int? organizacionId = null,
            string? orden = null)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ListarAsync(usuario, page, pageSize, q, categoriaId, organizacionId, orden);
            return result.ToHttpResult();
        }

        private static async Task<IResult> Obtener(
            int id,
            IActivoService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ObtenerAsync(id, usuario);
            return result.ToHttpResult();
        }

        private static async Task<IResult> Crear(CrearActivoRequest req, IActivoService service)
        {
            var dto = new CrearActivoDto(req.OrganizacionId, req.Nombre, req.HostnameIp, req.CategoriaId);
            var result = await service.CrearAsync(dto);
            return result.ToHttpResult(result.Success ? $"/api/v1/activos/{result.Data!.Id}" : null);
        }

        private static async Task<IResult> Actualizar(int id, ActualizarActivoRequest req, IActivoService service)
        {
            var dto = new ActualizarActivoDto(req.Nombre, req.HostnameIp, req.CategoriaId);
            var result = await service.ActualizarAsync(id, dto);
            return result.Success ? Results.Ok(new { mensaje = "Actualizado" }) : result.ToHttpResult();
        }

        private static async Task<IResult> Eliminar(int id, IActivoService service)
        {
            var result = await service.EliminarAsync(id);
            return result.Success ? Results.NoContent() : result.ToHttpResult();
        }

        record CrearActivoRequest(int OrganizacionId, string? Nombre, string? HostnameIp, int CategoriaId);
        record ActualizarActivoRequest(string? Nombre, string? HostnameIp, int CategoriaId);
    }
}
