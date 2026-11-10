using Microsoft.AspNetCore.Http;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    /// <summary>
    /// Adaptador HTTP de Minimal APIs para Organizaciones y Contratos SLA (ADR-002: Arquitectura en Capas).
    /// </summary>
    public static class OrganizacionAPI
    {
        public static void MapOrganizacionApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/organizaciones").WithTags("Organizaciones").RequireAuthorization();

            grupo.MapGet("", Listar).WithName("Listar organizaciones")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            grupo.MapGet("/{id}", Obtener).WithName("Obtener organizacion");
            grupo.MapPost("", Crear).WithName("Crear organizacion")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapPut("/{id}", Actualizar).WithName("Actualizar organizacion")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapDelete("/{id}", Eliminar).WithName("Eliminar organizacion")
                .RequireAuthorization(p => p.RequireRole("Administrador"));

            // Contratos SLA de la organizacion
            grupo.MapGet("/{id}/contratos-sla", ListarContratos).WithName("Listar contratos SLA");
            grupo.MapPost("/{id}/contratos-sla", CrearContrato).WithName("Crear contrato SLA")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapPost("/{id}/contratos-sla/{contratoId}/finalizar", FinalizarContrato).WithName("Finalizar contrato SLA")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapDelete("/{id}/contratos-sla/{contratoId}", EliminarContrato).WithName("Eliminar contrato SLA")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static async Task<IResult> Listar(
            IOrganizacionService service,
            int page = 1,
            int pageSize = 10,
            string? q = null,
            int? planId = null)
        {
            var result = await service.ListarAsync(page, pageSize, q, planId);
            return result.ToHttpResult();
        }

        private static async Task<IResult> Obtener(
            int id,
            IOrganizacionService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ObtenerAsync(id, usuario);
            return result.ToHttpResult();
        }

        private static async Task<IResult> Crear(OrganizacionRequest req, IOrganizacionService service)
        {
            var dto = new OrganizacionDto(req.RazonSocial, req.Rut, req.PlanId);
            var result = await service.CrearAsync(dto);
            return result.ToHttpResult(result.Success ? $"/api/v1/organizaciones/{result.Data!.Id}" : null);
        }

        private static async Task<IResult> Actualizar(int id, OrganizacionRequest req, IOrganizacionService service)
        {
            var dto = new OrganizacionDto(req.RazonSocial, req.Rut, req.PlanId);
            var result = await service.ActualizarAsync(id, dto);
            return result.Success ? Results.Ok(new { mensaje = "Actualizado" }) : result.ToHttpResult();
        }

        private static async Task<IResult> Eliminar(int id, IOrganizacionService service)
        {
            var result = await service.EliminarAsync(id);
            return result.Success ? Results.NoContent() : result.ToHttpResult();
        }

        private static async Task<IResult> ListarContratos(
            int id,
            IOrganizacionService service,
            SentryGuardDbContext db,
            HttpContext httpContext,
            AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var result = await service.ListarContratosAsync(id, usuario);
            return result.Success ? Results.Ok(new { total = result.Data!.Count(), items = result.Data }) : result.ToHttpResult();
        }

        private static async Task<IResult> CrearContrato(int id, CrearContratoRequest req, IOrganizacionService service)
        {
            var detalles = req.Detalles?.Select(d => new DetalleContratoDto(d.SeveridadId, d.HorasSla)).ToList();
            var dto = new CrearContratoDto(req.VigenteDesde, req.VigenteHasta, detalles);
            var result = await service.CrearContratoAsync(id, dto);
            return result.ToHttpResult(result.Success ? $"/api/v1/organizaciones/{id}/contratos-sla/{result.Data}" : null);
        }

        private static async Task<IResult> FinalizarContrato(int id, int contratoId, IOrganizacionService service)
        {
            var result = await service.FinalizarContratoAsync(id, contratoId);
            return result.Success ? Results.Ok(new { mensaje = "Contrato finalizado" }) : result.ToHttpResult();
        }

        private static async Task<IResult> EliminarContrato(int id, int contratoId, IOrganizacionService service)
        {
            var result = await service.EliminarContratoAsync(id, contratoId);
            return result.Success ? Results.NoContent() : result.ToHttpResult();
        }

        record OrganizacionRequest(string? RazonSocial, string? Rut, int PlanId);
        record CrearContratoRequest(DateTime VigenteDesde, DateTime? VigenteHasta, List<DetalleContratoRequest>? Detalles);
        record DetalleContratoRequest(int SeveridadId, decimal HorasSla);
    }
}
