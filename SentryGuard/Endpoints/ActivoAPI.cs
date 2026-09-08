using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Repositories;

namespace SentryGuard.Endpoints
{
    public static class ActivoAPI
    {
        public static void MapActivoApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/activos").WithTags("Activos").RequireAuthorization();
            grupo.MapGet("", Listar).WithName("Listar");
            grupo.MapGet("/{id}", Obtener).WithName("Obtener");
            grupo.MapPost("", Crear).WithName("Crear");
            grupo.MapPut("/{id}", Actualizar).WithName("Actualizar");
            grupo.MapDelete("/{id}", Eliminar).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static async Task<IResult> Listar(IActivoRepository repo, int page = 1, int pageSize = 10)
        {
            var activos = await repo.GetAllAsync();
            return Results.Ok(new { total = activos.Count(), page, pageSize, items = activos.Skip((page - 1) * pageSize).Take(pageSize) });
        }

        private static async Task<IResult> Obtener(int id, IActivoRepository repo)
        {
            var activo = await repo.GetByIdAsync(id);
            return activo == null ? Results.NotFound() : Results.Ok(activo);
        }

        private static async Task<IResult> Crear(CrearActivoRequest req, IActivoRepository repo)
        {
            var activo = new Models.Activo { OrganizacionId = 1, Nombre = req.Nombre, HostnameIp = req.HostnameIp, CategoriaId = req.CategoriaId };
            await repo.AddAsync(activo);
            return Results.Created($"/api/activos/{activo.Id}", activo);
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

        record CrearActivoRequest(string Nombre, string? HostnameIp, int CategoriaId);
        record ActualizarActivoRequest(string? Nombre);
    }
}
