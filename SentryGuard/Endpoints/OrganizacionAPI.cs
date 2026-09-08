using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Repositories;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    public static class OrganizacionAPI
    {
        public static void MapOrganizacionApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/organizaciones").WithTags("Organizaciones").RequireAuthorization();
            // Listar todas las organizaciones es solo para staff de CyberOps; un Cliente no necesita ver otras.
            grupo.MapGet("", Listar).WithName("Listar organizaciones")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Analista"));
            // Obtener por id: abierto a los 4 roles, pero Cliente solo puede pedir la suya (validado dentro del handler).
            grupo.MapGet("/{id}", Obtener).WithName("Obtener organizacion");
            grupo.MapPost("", Crear).RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapPut("/{id}", Actualizar).RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapDelete("/{id}", Eliminar).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static async Task<IResult> Listar(SentryGuardDbContext db, int page = 1, int pageSize = 10)
        {
            var total = await db.Organizaciones.CountAsync();
            var items = await db.Organizaciones
                .OrderBy(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrganizacionDTO
                {
                    Id = o.Id,
                    RazonSocial = o.RazonSocial,
                    Rut = o.Rut,
                    Plan = o.Plan!.Nombre,
                    FechaAlta = o.FechaAlta
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        }

        private static async Task<IResult> Obtener(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            if (usuario.Rol?.Nombre == "Cliente" && usuario.OrganizacionId != id)
                return Results.Forbid();

            var dto = await db.Organizaciones
                .Where(o => o.Id == id)
                .Select(o => new OrganizacionDTO { Id = o.Id, RazonSocial = o.RazonSocial, Rut = o.Rut, Plan = o.Plan!.Nombre, FechaAlta = o.FechaAlta })
                .FirstOrDefaultAsync();

            return dto == null ? Results.NotFound() : Results.Ok(dto);
        }

        private static async Task<IResult> Crear(CrearOrgRequest req, IOrganizacionRepository repo, SentryGuardDbContext db)
        {
            var plan = await db.PlanesServicio.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.PlanId);
            if (plan == null) return Results.BadRequest(new { error = "Plan no existe" });

            var org = new Models.Organizacion { RazonSocial = req.RazonSocial, Rut = req.Rut, PlanContratadoId = req.PlanId, FechaAlta = DateTime.UtcNow };
            await repo.AddAsync(org);

            var dto = new OrganizacionDTO { Id = org.Id, RazonSocial = org.RazonSocial, Rut = org.Rut, Plan = plan.Nombre, FechaAlta = org.FechaAlta };
            return Results.Created($"/api/v1/organizaciones/{org.Id}", dto);
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
