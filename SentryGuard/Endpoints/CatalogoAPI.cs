using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;

namespace SentryGuard.Endpoints
{
    public static class CatalogoAPI
    {
        public static void MapCatalogoApi(this WebApplication app)
        {
            var catalogo = app.MapGroup("/api/catalogos")
                .WithTags("Catálogos");

            // Severidades
            catalogo.MapGet("/severidades", ListarSeveridades)
                .WithName("Listar severidades")
                .WithDescription("Lista todas las severidades");

            catalogo.MapPost("/severidades", CrearSeveridad)
                .WithName("Crear severidad")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            // Estados de incidente
            catalogo.MapGet("/estados-incidente", ListarEstadosIncidente)
                .WithName("Listar estados")
                .WithDescription("Lista todos los estados de incidente");

            catalogo.MapPost("/estados-incidente", CrearEstadoIncidente)
                .WithName("Crear estado")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            // Categorías de activo
            catalogo.MapGet("/categorias-activo", ListarCategoriasActivo)
                .WithName("Listar categorías")
                .WithDescription("Lista todas las categorías de activo");

            catalogo.MapPost("/categorias-activo", CrearCategoriaActivo)
                .WithName("Crear categoría")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            // Planes de servicio
            catalogo.MapGet("/planes-servicio", ListarPlanesServicio)
                .WithName("Listar planes")
                .WithDescription("Lista todos los planes de servicio");

            catalogo.MapPost("/planes-servicio", CrearPlanServicio)
                .WithName("Crear plan")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            // Roles
            catalogo.MapGet("/roles", ListarRoles)
                .WithName("Listar roles")
                .WithDescription("Lista todos los roles disponibles");
        }

        private static async Task<IResult> ListarSeveridades(
            SentryGuardDbContext db,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return Results.BadRequest(new { error = "Parámetros inválidos" });

            var total = await db.Severidades.CountAsync();
            var items = await db.Severidades
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = Math.Ceiling((double)total / pageSize),
                items = items.Select(s => new { s.Id, s.Nombre, horasSLA = s.HorasSla })
            });
        }

        private static async Task<IResult> CrearSeveridad(
            CrearSeveridadRequest request,
            SentryGuardDbContext db)
        {
            var severidad = new Models.Severidad
            {
                Nombre = request.Nombre,
                HorasSla = request.HorasSla
            };

            db.Severidades.Add(severidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogos/severidades/{severidad.Id}", new
            {
                id = severidad.Id,
                nombre = severidad.Nombre
            });
        }

        private static async Task<IResult> ListarEstadosIncidente(
            SentryGuardDbContext db,
            int page = 1,
            int pageSize = 10)
        {
            var total = await db.EstadosIncidente.CountAsync();
            var items = await db.EstadosIncidente
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = Math.Ceiling((double)total / pageSize),
                items = items.Select(e => new { e.Id, e.Nombre })
            });
        }

        private static async Task<IResult> CrearEstadoIncidente(
            CrearEstadoRequest request,
            SentryGuardDbContext db)
        {
            var estado = new Models.EstadoIncidente
            {
                Nombre = request.Nombre
            };

            db.EstadosIncidente.Add(estado);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogos/estados-incidente/{estado.Id}", new
            {
                id = estado.Id,
                nombre = estado.Nombre
            });
        }

        private static async Task<IResult> ListarCategoriasActivo(
            SentryGuardDbContext db,
            int page = 1,
            int pageSize = 10)
        {
            var total = await db.CategoriasActivo.CountAsync();
            var items = await db.CategoriasActivo
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = Math.Ceiling((double)total / pageSize),
                items = items.Select(c => new { c.Id, c.Nombre })
            });
        }

        private static async Task<IResult> CrearCategoriaActivo(
            CrearCategoriaRequest request,
            SentryGuardDbContext db)
        {
            var categoria = new Models.CategoriaActivo
            {
                Nombre = request.Nombre
            };

            db.CategoriasActivo.Add(categoria);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogos/categorias-activo/{categoria.Id}", new
            {
                id = categoria.Id,
                nombre = categoria.Nombre
            });
        }

        private static async Task<IResult> ListarPlanesServicio(
            SentryGuardDbContext db,
            int page = 1,
            int pageSize = 10)
        {
            var total = await db.PlanesServicio.CountAsync();
            var items = await db.PlanesServicio
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = Math.Ceiling((double)total / pageSize),
                items = items.Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    tiempoRespuestaBaseH = p.TiempoRespuestaBaseH
                })
            });
        }

        private static async Task<IResult> CrearPlanServicio(
            CrearPlanRequest request,
            SentryGuardDbContext db)
        {
            var plan = new Models.PlanServicio
            {
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                TiempoRespuestaBaseH = request.TiempoRespuestaBaseH
            };

            db.PlanesServicio.Add(plan);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogos/planes-servicio/{plan.Id}", new
            {
                id = plan.Id,
                nombre = plan.Nombre
            });
        }

        private static async Task<IResult> ListarRoles(SentryGuardDbContext db)
        {
            var roles = await db.Roles.ToListAsync();
            return Results.Ok(new
            {
                total = roles.Count(),
                items = roles.Select(r => new { r.Id, r.Nombre })
            });
        }

        // DTOs
        record CrearSeveridadRequest(string Nombre, decimal HorasSla);
        record CrearEstadoRequest(string Nombre);
        record CrearCategoriaRequest(string Nombre);
        record CrearPlanRequest(string Nombre, string? Descripcion, decimal TiempoRespuestaBaseH);
    }
}
