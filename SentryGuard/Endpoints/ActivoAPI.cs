using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Repositories;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    public static class ActivoAPI
    {
        public static void MapActivoApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/activos").WithTags("Activos").RequireAuthorization();
            // Listar/Obtener: los 4 roles, filtrado por organizacion dentro del handler para Cliente.
            grupo.MapGet("", Listar).WithName("Listar activos");
            grupo.MapGet("/{id}", Obtener).WithName("Obtener activo");
            // Crear/Actualizar/Eliminar: solo staff de CyberOps (Administrador/Supervisor).
            grupo.MapPost("", Crear).WithName("Crear activo")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            grupo.MapPut("/{id}", Actualizar).WithName("Actualizar activo")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            grupo.MapDelete("/{id}", Eliminar).WithName("Eliminar activo")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static async Task<IResult> Listar(SentryGuardDbContext db, HttpContext httpContext, AuthService authService, int page = 1, int pageSize = 10)
        {
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            // Cliente solo ve los activos de su propia organizacion (OWASP API1); el resto ve todos.
            IQueryable<Models.Activo> query = db.Activos;
            if (usuario.Rol?.Nombre == "Cliente")
                query = query.Where(a => a.OrganizacionId == usuario.OrganizacionId);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActivoDTO
                {
                    Id = a.Id,
                    Nombre = a.Nombre,
                    HostnameIp = a.HostnameIp,
                    Categoria = a.Categoria!.Nombre
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        }

        private static async Task<IResult> Obtener(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            // Consulta plana solo para validar acceso (nunca se serializa, evita el fixup de navegacion de EF).
            var activo = await db.Activos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (activo == null) return Results.NotFound();

            if (usuario.Rol?.Nombre == "Cliente" && usuario.OrganizacionId != activo.OrganizacionId)
                return Results.Forbid();

            var dto = await db.Activos
                .Where(a => a.Id == id)
                .Select(a => new ActivoDTO { Id = a.Id, Nombre = a.Nombre, HostnameIp = a.HostnameIp, Categoria = a.Categoria!.Nombre })
                .FirstAsync();

            return Results.Ok(dto);
        }

        private static async Task<IResult> Crear(CrearActivoRequest req, IActivoRepository repo, SentryGuardDbContext db)
        {
            var organizacion = await db.Organizaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == req.OrganizacionId);
            if (organizacion == null) return Results.BadRequest(new { error = "Organizacion no existe" });

            var categoria = await db.CategoriasActivo.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.CategoriaId);
            if (categoria == null) return Results.BadRequest(new { error = "Categoria no existe" });

            var activo = new Models.Activo { OrganizacionId = req.OrganizacionId, Nombre = req.Nombre, HostnameIp = req.HostnameIp, CategoriaId = req.CategoriaId };
            await repo.AddAsync(activo);

            var dto = new ActivoDTO { Id = activo.Id, Nombre = activo.Nombre, HostnameIp = activo.HostnameIp, Categoria = categoria.Nombre };
            return Results.Created($"/api/v1/activos/{activo.Id}", dto);
        }

        private static async Task<IResult> Actualizar(int id, ActualizarActivoRequest req, IActivoRepository repo)
        {
            var activo = await repo.GetByIdAsync(id);
            if (activo == null) return Results.NotFound();
            if (!string.IsNullOrEmpty(req.Nombre)) activo.Nombre = req.Nombre;
            await repo.UpdateAsync(activo);
            return Results.Ok();
        }

        private static async Task<IResult> Eliminar(int id, IActivoRepository repo)
        {
            var activo = await repo.GetByIdAsync(id);
            if (activo == null) return Results.NotFound();
            await repo.DeleteAsync(id);
            return Results.NoContent();
        }

        record CrearActivoRequest(int OrganizacionId, string Nombre, string? HostnameIp, int CategoriaId);
        record ActualizarActivoRequest(string? Nombre);
    }
}
