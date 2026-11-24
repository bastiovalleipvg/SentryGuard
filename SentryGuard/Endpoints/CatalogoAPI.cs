using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    public static class CatalogoAPI
    {
        public static void MapCatalogoApi(this WebApplication app)
        {
            var catalogo = app.MapGroup("/api/v1/catalogos")
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

            // Flujo de estados como datos (ADR-008): la interfaz solo ofrece estos destinos
            catalogo.MapGet("/transiciones-estado", async (IIncidenteService incidentes) =>
                    Results.Ok(await incidentes.ListarTransicionesAsync()))
                .WithName("Listar transiciones de estado")
                .WithDescription("Pares (origen, destino) permitidos del flujo de estados del incidente")
                .RequireAuthorization();

            // Categorías de activo
            catalogo.MapGet("/categorias-activo", ListarCategoriasActivo)
                .WithName("Listar categorías")
                .WithDescription("Lista todas las categorías de activo");

            catalogo.MapPost("/categorias-activo", CrearCategoriaActivo)
                .WithName("Crear categoría")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            // Categorías de incidente (RF-15)
            catalogo.MapGet("/categorias-incidente", ListarCategoriasIncidente)
                .WithName("Listar categorías de incidente")
                .WithDescription("Lista todas las categorías de incidente");

            catalogo.MapPost("/categorias-incidente", CrearCategoriaIncidente)
                .WithName("Crear categoría de incidente")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            catalogo.MapPut("/categorias-incidente/{id}", ActualizarCategoriaIncidente)
                .WithName("Actualizar categoría de incidente")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            catalogo.MapDelete("/categorias-incidente/{id}", EliminarCategoriaIncidente)
                .WithName("Eliminar categoría de incidente")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            // Planes de servicio
            catalogo.MapGet("/planes-servicio", ListarPlanesServicio)
                .WithName("Listar planes")
                .WithDescription("Lista todos los planes de servicio");

            catalogo.MapPost("/planes-servicio", CrearPlanServicio)
                .WithName("Crear plan")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));

            catalogo.MapPut("/severidades/{id}", ActualizarSeveridad).WithName("Actualizar severidad")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));
            catalogo.MapDelete("/severidades/{id}", EliminarSeveridad).WithName("Eliminar severidad")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));
            catalogo.MapPut("/categorias-activo/{id}", ActualizarCategoriaActivo).WithName("Actualizar categoría")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));
            catalogo.MapDelete("/categorias-activo/{id}", EliminarCategoriaActivo).WithName("Eliminar categoría")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));
            catalogo.MapPut("/planes-servicio/{id}", ActualizarPlanServicio).WithName("Actualizar plan")
                .RequireAuthorization(policy => policy.RequireRole("Administrador"));
            catalogo.MapDelete("/planes-servicio/{id}", EliminarPlanServicio).WithName("Eliminar plan")
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
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);

            var total = await db.Severidades.CountAsync();
            var items = await db.Severidades
                .OrderBy(x => x.Id)
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
            var errores = await ValidarNombreAsync(request.Nombre, n => db.Severidades.AnyAsync(x => x.Nombre == n));
            Validacion.Horas(errores, "horasSla", request.HorasSla);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var severidad = new Models.Severidad
            {
                Nombre = request.Nombre!.Trim(),
                HorasSla = request.HorasSla
            };

            db.Severidades.Add(severidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/catalogos/severidades/{severidad.Id}", new
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
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);
            var total = await db.EstadosIncidente.CountAsync();
            var items = await db.EstadosIncidente
                .OrderBy(x => x.Id)
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
            var errores = await ValidarNombreAsync(request.Nombre, n => db.EstadosIncidente.AnyAsync(x => x.Nombre == n));
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var estado = new Models.EstadoIncidente
            {
                Nombre = request.Nombre!.Trim()
            };

            db.EstadosIncidente.Add(estado);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/catalogos/estados-incidente/{estado.Id}", new
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
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);
            var total = await db.CategoriasActivo.CountAsync();
            var items = await db.CategoriasActivo
                .OrderBy(x => x.Id)
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
            var errores = await ValidarNombreAsync(request.Nombre, n => db.CategoriasActivo.AnyAsync(x => x.Nombre == n));
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var categoria = new Models.CategoriaActivo
            {
                Nombre = request.Nombre!.Trim()
            };

            db.CategoriasActivo.Add(categoria);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/catalogos/categorias-activo/{categoria.Id}", new
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
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);
            var total = await db.PlanesServicio.CountAsync();
            var items = await db.PlanesServicio
                .OrderBy(x => x.Id)
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
            var errores = await ValidarNombreAsync(request.Nombre, n => db.PlanesServicio.AnyAsync(x => x.Nombre == n));
            Validacion.Texto(errores, "descripcion", request.Descripcion, 0, 500, requerido: false);
            Validacion.Horas(errores, "tiempoRespuestaBaseH", request.TiempoRespuestaBaseH);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var plan = new Models.PlanServicio
            {
                Nombre = request.Nombre!.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
                TiempoRespuestaBaseH = request.TiempoRespuestaBaseH
            };

            db.PlanesServicio.Add(plan);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/catalogos/planes-servicio/{plan.Id}", new
            {
                id = plan.Id,
                nombre = plan.Nombre
            });
        }

        // Nombre obligatorio (2 a 50 caracteres) y unico en su catalogo (la collation de SQL Server ignora mayusculas).
        private static async Task<Dictionary<string, string[]>> ValidarNombreAsync(string? nombre, Func<string, Task<bool>> yaExiste)
        {
            var errores = new Dictionary<string, string[]>();
            Validacion.Texto(errores, "nombre", nombre, 2, 50, requerido: true);
            if (errores.Count == 0 && await yaExiste(nombre!.Trim()))
                errores["nombre"] = ["Ya existe un elemento con ese nombre."];
            return errores;
        }

        private static async Task<IResult> ActualizarSeveridad(int id, CrearSeveridadRequest request, SentryGuardDbContext db)
        {
            var severidad = await db.Severidades.FindAsync(id);
            if (severidad == null) return Results.NotFound();

            var errores = await ValidarNombreAsync(request.Nombre, n => db.Severidades.AnyAsync(x => x.Nombre == n && x.Id != id));
            Validacion.Horas(errores, "horasSla", request.HorasSla);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            severidad.Nombre = request.Nombre!.Trim();
            severidad.HorasSla = request.HorasSla;
            await db.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        private static async Task<IResult> EliminarSeveridad(int id, SentryGuardDbContext db)
        {
            var severidad = await db.Severidades.FindAsync(id);
            if (severidad == null) return Results.NotFound();
            if (await db.Incidentes.AnyAsync(i => i.SeveridadId == id) || await db.ContratosSlaDetalle.AnyAsync(d => d.SeveridadId == id))
                return Validacion.Conflicto("EN_USO", "No se puede eliminar: hay incidentes o contratos SLA que usan esta severidad.");

            db.Severidades.Remove(severidad);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }

        private static async Task<IResult> ActualizarCategoriaActivo(int id, CrearCategoriaRequest request, SentryGuardDbContext db)
        {
            var categoria = await db.CategoriasActivo.FindAsync(id);
            if (categoria == null) return Results.NotFound();

            var errores = await ValidarNombreAsync(request.Nombre, n => db.CategoriasActivo.AnyAsync(x => x.Nombre == n && x.Id != id));
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            categoria.Nombre = request.Nombre!.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        private static async Task<IResult> EliminarCategoriaActivo(int id, SentryGuardDbContext db)
        {
            var categoria = await db.CategoriasActivo.FindAsync(id);
            if (categoria == null) return Results.NotFound();
            if (await db.Activos.AnyAsync(a => a.CategoriaId == id))
                return Validacion.Conflicto("EN_USO", "No se puede eliminar: hay activos de esta categoría.");

            db.CategoriasActivo.Remove(categoria);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }

        private static async Task<IResult> ActualizarPlanServicio(int id, CrearPlanRequest request, SentryGuardDbContext db)
        {
            var plan = await db.PlanesServicio.FindAsync(id);
            if (plan == null) return Results.NotFound();

            var errores = await ValidarNombreAsync(request.Nombre, n => db.PlanesServicio.AnyAsync(x => x.Nombre == n && x.Id != id));
            Validacion.Texto(errores, "descripcion", request.Descripcion, 0, 500, requerido: false);
            Validacion.Horas(errores, "tiempoRespuestaBaseH", request.TiempoRespuestaBaseH);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            plan.Nombre = request.Nombre!.Trim();
            plan.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
            plan.TiempoRespuestaBaseH = request.TiempoRespuestaBaseH;
            await db.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        private static async Task<IResult> EliminarPlanServicio(int id, SentryGuardDbContext db)
        {
            var plan = await db.PlanesServicio.FindAsync(id);
            if (plan == null) return Results.NotFound();
            if (await db.Organizaciones.AnyAsync(o => o.PlanContratadoId == id))
                return Validacion.Conflicto("EN_USO", "No se puede eliminar: hay organizaciones con este plan contratado.");

            db.PlanesServicio.Remove(plan);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }

        private static async Task<IResult> ListarCategoriasIncidente(
            SentryGuardDbContext db,
            int page = 1,
            int pageSize = 50)
        {
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);

            var total = await db.CategoriasIncidente.CountAsync();
            var items = await db.CategoriasIncidente
                .OrderBy(c => c.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    id = c.Id,
                    nombre = c.Nombre,
                    descripcion = c.Descripcion
                })
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                items
            });
        }

        private static async Task<IResult> CrearCategoriaIncidente(
            CrearCategoriaRequest request,
            SentryGuardDbContext db)
        {
            var errores = await ValidarNombreAsync(request.Nombre, n => db.CategoriasIncidente.AnyAsync(c => c.Nombre == n));
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var categoria = new Models.CategoriaIncidente
            {
                Nombre = request.Nombre!.Trim()
            };

            db.CategoriasIncidente.Add(categoria);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/catalogos/categorias-incidente/{categoria.Id}", new
            {
                id = categoria.Id,
                nombre = categoria.Nombre
            });
        }

        private static async Task<IResult> ActualizarCategoriaIncidente(int id, CrearCategoriaRequest request, SentryGuardDbContext db)
        {
            var categoria = await db.CategoriasIncidente.FindAsync(id);
            if (categoria == null) return Results.NotFound();

            var errores = await ValidarNombreAsync(request.Nombre, n => db.CategoriasIncidente.AnyAsync(c => c.Nombre == n && c.Id != id));
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            categoria.Nombre = request.Nombre!.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        private static async Task<IResult> EliminarCategoriaIncidente(int id, SentryGuardDbContext db)
        {
            var categoria = await db.CategoriasIncidente.FindAsync(id);
            if (categoria == null) return Results.NotFound();

            if (await db.Incidentes.AnyAsync(i => i.CategoriaId == id))
                return Validacion.Conflicto("EN_USO", "No se puede eliminar: hay incidentes asociados a esta categoría.");

            db.CategoriasIncidente.Remove(categoria);
            await db.SaveChangesAsync();
            return Results.NoContent();
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
        record CrearSeveridadRequest(string? Nombre, decimal HorasSla);
        record CrearEstadoRequest(string? Nombre);
        record CrearCategoriaRequest(string? Nombre);
        record CrearPlanRequest(string? Nombre, string? Descripcion, decimal TiempoRespuestaBaseH);
    }
}
