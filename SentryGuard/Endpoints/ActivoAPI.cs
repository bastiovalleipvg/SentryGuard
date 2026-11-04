using System.Linq.Expressions;
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

        private const int NombreMin = 3;
        private const int NombreMax = 150;
        private const int BusquedaMax = 100;

        private static readonly Expression<Func<Models.Activo, ActivoDTO>> ADto = a => new ActivoDTO
        {
            Id = a.Id,
            Nombre = a.Nombre,
            HostnameIp = a.HostnameIp,
            CategoriaId = a.CategoriaId,
            Categoria = a.Categoria!.Nombre,
            OrganizacionId = a.OrganizacionId,
            Organizacion = a.Organizacion!.RazonSocial
        };

        private static async Task<IResult> Listar(SentryGuardDbContext db, HttpContext httpContext, AuthService authService,
            int page = 1, int pageSize = 10, string? q = null, int? categoriaId = null, int? organizacionId = null)
        {
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            // Cliente: solo los activos de su organizacion. Analista: solo los activos de sus incidentes asignados.
            // En ambos casos el filtro de organizacion que manden se ignora (OWASP API1).
            IQueryable<Models.Activo> query = db.Activos;
            if (usuario.Rol?.Nombre == "Cliente")
                query = query.Where(a => a.OrganizacionId == usuario.OrganizacionId);
            else if (usuario.Rol?.Nombre == "Analista")
                query = query.Where(a => db.Incidentes.Any(i => i.ActivoId == a.Id && i.AnalistaAsignadoId == usuario.Id));
            else if (organizacionId.HasValue)
                query = query.Where(a => a.OrganizacionId == organizacionId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                if (termino.Length > BusquedaMax) termino = termino[..BusquedaMax];
                query = query.Where(a => a.Nombre.Contains(termino) || (a.HostnameIp != null && a.HostnameIp.Contains(termino)));
            }
            if (categoriaId.HasValue) query = query.Where(a => a.CategoriaId == categoriaId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(a => a.Nombre)
                .ThenBy(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ADto)
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize), items });
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
            if (usuario.Rol?.Nombre == "Analista" && !await db.Incidentes.AnyAsync(i => i.ActivoId == id && i.AnalistaAsignadoId == usuario.Id))
                return Results.Forbid();

            var dto = await db.Activos.Where(a => a.Id == id).Select(ADto).FirstAsync();
            return Results.Ok(dto);
        }

        private static async Task ValidarAsync(Dictionary<string, string[]> errores, SentryGuardDbContext db, string? nombre, string? hostnameIp, int categoriaId)
        {
            Validacion.Texto(errores, "nombre", nombre, NombreMin, NombreMax, requerido: true);
            if (!string.IsNullOrWhiteSpace(hostnameIp) && !Validacion.EsHostnameOIp(hostnameIp.Trim()))
                errores["hostnameIp"] = ["Usa un nombre de host o una IP válida (letras, números, punto, guion o dos puntos)."];
            if (!await db.CategoriasActivo.AnyAsync(c => c.Id == categoriaId))
                errores["categoriaId"] = ["La categoría seleccionada no existe."];
        }

        private static async Task<IResult> Crear(CrearActivoRequest req, IActivoRepository repo, SentryGuardDbContext db)
        {
            var errores = new Dictionary<string, string[]>();
            await ValidarAsync(errores, db, req.Nombre, req.HostnameIp, req.CategoriaId);
            if (!await db.Organizaciones.AnyAsync(o => o.Id == req.OrganizacionId))
                errores["organizacionId"] = ["La organización seleccionada no existe."];
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var activo = new Models.Activo
            {
                OrganizacionId = req.OrganizacionId,
                Nombre = req.Nombre!.Trim(),
                HostnameIp = string.IsNullOrWhiteSpace(req.HostnameIp) ? null : req.HostnameIp.Trim(),
                CategoriaId = req.CategoriaId
            };
            await repo.AddAsync(activo);

            var dto = await db.Activos.Where(a => a.Id == activo.Id).Select(ADto).FirstAsync();
            return Results.Created($"/api/v1/activos/{activo.Id}", dto);
        }

        // La organizacion no se cambia: los incidentes del activo pertenecen a ella.
        private static async Task<IResult> Actualizar(int id, ActualizarActivoRequest req, IActivoRepository repo, SentryGuardDbContext db)
        {
            var activo = await repo.GetByIdAsync(id);
            if (activo == null) return Results.NotFound();

            var errores = new Dictionary<string, string[]>();
            await ValidarAsync(errores, db, req.Nombre, req.HostnameIp, req.CategoriaId);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            activo.Nombre = req.Nombre!.Trim();
            activo.HostnameIp = string.IsNullOrWhiteSpace(req.HostnameIp) ? null : req.HostnameIp.Trim();
            activo.CategoriaId = req.CategoriaId;
            await repo.UpdateAsync(activo);
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        private static async Task<IResult> Eliminar(int id, IActivoRepository repo, SentryGuardDbContext db)
        {
            var activo = await repo.GetByIdAsync(id);
            if (activo == null) return Results.NotFound();

            // La FK es Restrict: sin este control el DELETE terminaria en un 500.
            if (await db.Incidentes.AnyAsync(i => i.ActivoId == id))
                return Validacion.Conflicto("ACTIVO_CON_INCIDENTES", "No se puede eliminar el activo porque tiene incidentes registrados.");

            await repo.DeleteAsync(id);
            return Results.NoContent();
        }

        record CrearActivoRequest(int OrganizacionId, string? Nombre, string? HostnameIp, int CategoriaId);
        record ActualizarActivoRequest(string? Nombre, string? HostnameIp, int CategoriaId);
    }
}
