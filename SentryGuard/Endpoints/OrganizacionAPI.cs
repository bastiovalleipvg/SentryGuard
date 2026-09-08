using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Repositories;

namespace SentryGuard.Endpoints
{
    public static class OrganizacionAPI
    {
        public static void MapOrganizacionApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/organizaciones").WithTags("Organizaciones").RequireAuthorization();
            grupo.MapGet("", Listar).WithName("Listar organizaciones");
            grupo.MapGet("/{id}", Obtener).WithName("Obtener organizacion");
            grupo.MapPost("", Crear).RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapPut("/{id}", Actualizar).RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapDelete("/{id}", Eliminar).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static async Task<IResult> Listar(IOrganizacionRepository repo, int page = 1, int pageSize = 10)
        {
            var orgs = await repo.GetAllAsync();
            return Results.Ok(new { total = orgs.Count(), page, pageSize, items = orgs.Skip((page - 1) * pageSize).Take(pageSize) });
        }

        private static async Task<IResult> Obtener(int id, IOrganizacionRepository repo)
        {
            var org = await repo.GetByIdAsync(id);
            return org == null ? Results.NotFound() : Results.Ok(org);
        }

        private static async Task<IResult> Crear(CrearOrgRequest req, IOrganizacionRepository repo)
        {
            var org = new Models.Organizacion { RazonSocial = req.RazonSocial, Rut = req.Rut, PlanContratadoId = req.PlanId, FechaAlta = DateTime.UtcNow };
            await repo.AddAsync(org);
            return Results.Created($"/api/organizaciones/{org.Id}", org);
        }

        private static async Task<IResult> Actualizar(int id, ActualizarOrgRequest req, IOrganizacionRepository repo)
        {
            var org = await repo.GetByIdAsync(id);
            if (org == null) return Results.NotFound();
            if (!string.IsNullOrEmpty(req.RazonSocial)) org.RazonSocial = req.RazonSocial;
            await repo.UpdateAsync(org);
            return Results.Ok();
        }

        private static async Task<IResult> Eliminar(int id, IOrganizacionRepository repo)
        {
            var org = await repo.GetByIdAsync(id);
            if (org == null) return Results.NotFound();
            await repo.DeleteAsync(id);
            return Results.NoContent();
        }

        record CrearOrgRequest(string RazonSocial, string Rut, int PlanId);
        record ActualizarOrgRequest(string? RazonSocial);
    }
}
