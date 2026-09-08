using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Repositories;
using SentryGuard.Services;
using System.Security.Claims;

namespace SentryGuard.Endpoints
{
    public static class IncidenteAPI
    {
        public static void MapIncidenteApi(this WebApplication app)
        {
            var incidentes = app.MapGroup("/api/v1/incidentes")
                .WithTags("Incidentes")
                .RequireAuthorization();

            // Cliente crea sus propios incidentes; Administrador/Supervisor pueden crearlos por cualquier organizacion.
            // Rate limiting por IP para mitigar spam/abuso en la creacion de incidentes (OWASP API4).
            incidentes.MapPost("", CrearIncidente).WithName("Crear incidente")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Cliente"))
                .RequireRateLimiting("incidentes-crear");
            // Listar/Obtener/Bitacora/Evidencia/SLA: abiertos a los 4 roles, filtrados/validados dentro del handler.
            incidentes.MapGet("", ListarIncidentes).WithName("Listar incidentes");
            incidentes.MapGet("/{id}", ObtenerIncidente).WithName("Obtener incidente");
            // Analista solo si esta asignado (validado dentro del handler); Cliente no edita incidentes.
            incidentes.MapPut("/{id}", ActualizarIncidente).WithName("Actualizar incidente")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Analista"));
            // Eliminar es destructivo: solo Administrador.
            incidentes.MapDelete("/{id}", EliminarIncidente).WithName("Eliminar incidente")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            // "Supervisor asigna/reasigna incidentes" (spec del proyecto).
            incidentes.MapPost("/{id}/asignar", AsignarAnalista).WithName("Asignar analista")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            incidentes.MapPost("/{id}/cambiar-estado", CambiarEstado).WithName("Cambiar estado")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Analista"));
            incidentes.MapPost("/{id}/bitacora", AgregarBitacora).WithName("Agregar bitácora")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Analista"));
            incidentes.MapGet("/{id}/bitacora", ObtenerBitacora).WithName("Obtener bitácora");
            incidentes.MapPost("/{id}/evidencia", AgregarEvidencia).WithName("Agregar evidencia")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor", "Analista"));
            incidentes.MapGet("/{id}/evidencia", ObtenerEvidencia).WithName("Obtener evidencia");
            incidentes.MapGet("/sla/cumplimiento/{id}", ConsultarCumplimientoSla).WithName("Cumplimiento SLA");
        }

        private static async Task<IResult> CrearIncidente(CrearIncidenteRequest request, IIncidenteRepository repo, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var activo = await db.Activos.FindAsync(request.ActivoId);
            if (activo == null) return Results.BadRequest(new { error = "Activo no existe" });

            // El incidente pertenece a la organizacion dueña del activo (OWASP API1: nunca confiar en un id del body).
            if (usuario.Rol?.Nombre == "Cliente" && usuario.OrganizacionId != activo.OrganizacionId)
                return Results.Forbid();

            var estado = await db.EstadosIncidente.FirstOrDefaultAsync(e => e.Nombre == "Abierto");
            var incidente = new Models.Incidente
            {
                OrganizacionId = activo.OrganizacionId,
                ActivoId = request.ActivoId,
                Titulo = request.Titulo,
                Descripcion = request.Descripcion,
                SeveridadId = request.SeveridadId,
                EstadoId = estado?.Id ?? 1,
                FechaCreacion = DateTime.UtcNow
            };

            await repo.AddAsync(incidente);
            return Results.Created($"/api/v1/incidentes/{incidente.Id}", new { id = incidente.Id, titulo = incidente.Titulo });
        }

        private static async Task<IResult> ListarIncidentes(SentryGuardDbContext db, HttpContext httpContext, AuthService authService, int page = 1, int pageSize = 10)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            IQueryable<Models.Incidente> query = db.Incidentes;

            // Cliente: solo su organizacion. Analista: solo lo asignado. Administrador/Supervisor: todo.
            if (usuario.Rol?.Nombre == "Cliente")
                query = query.Where(i => i.OrganizacionId == usuario.OrganizacionId);
            else if (usuario.Rol?.Nombre == "Analista")
                query = query.Where(i => i.AnalistaAsignadoId == usuario.Id);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new IncidenteDTO
                {
                    Id = i.Id,
                    Titulo = i.Titulo,
                    Descripcion = i.Descripcion,
                    Severidad = i.Severidad!.Nombre,
                    Estado = i.Estado!.Nombre,
                    Activo = i.Activo!.Nombre,
                    AnalistaAsignado = i.AnalistaAsignado != null ? i.AnalistaAsignado.Nombre : null,
                    FechaCreacion = i.FechaCreacion,
                    FechaCierre = i.FechaCierre
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, totalPages = Math.Ceiling((double)total / pageSize), items });
        }

        private static async Task<IResult> ObtenerIncidente(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            // Consulta plana solo para validar acceso (nunca se serializa, evita exponer navegaciones completas).
            var incidente = await db.Incidentes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var dto = await db.Incidentes
                .Where(i => i.Id == id)
                .Select(i => new IncidenteDTO
                {
                    Id = i.Id,
                    Titulo = i.Titulo,
                    Descripcion = i.Descripcion,
                    Severidad = i.Severidad!.Nombre,
                    Estado = i.Estado!.Nombre,
                    Activo = i.Activo!.Nombre,
                    AnalistaAsignado = i.AnalistaAsignado != null ? i.AnalistaAsignado.Nombre : null,
                    FechaCreacion = i.FechaCreacion,
                    FechaCierre = i.FechaCierre
                })
                .FirstAsync();

            return Results.Ok(dto);
        }

        private static async Task<IResult> ActualizarIncidente(int id, ActualizarIncidenteRequest request, IIncidenteRepository repo, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await repo.GetByIdAsync(id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            if (!string.IsNullOrEmpty(request.Titulo)) incidente.Titulo = request.Titulo;
            if (!string.IsNullOrEmpty(request.Descripcion)) incidente.Descripcion = request.Descripcion;

            await repo.UpdateAsync(incidente);
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        private static async Task<IResult> EliminarIncidente(int id, IIncidenteRepository repo)
        {
            var incidente = await repo.GetByIdAsync(id);
            if (incidente == null) return Results.NotFound();
            await repo.DeleteAsync(id);
            return Results.NoContent();
        }

        private static async Task<IResult> AsignarAnalista(int id, AsignarAnalistaRequest request, IIncidenteRepository repo, SentryGuardDbContext db)
        {
            var incidente = await repo.GetByIdAsync(id);
            if (incidente == null) return Results.NotFound();

            var analista = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == request.AnalistaId);
            if (analista == null || analista.Rol?.Nombre != "Analista")
                return Results.BadRequest(new { error = "El usuario indicado no existe o no tiene rol Analista" });

            incidente.AnalistaAsignadoId = request.AnalistaId;
            await repo.UpdateAsync(incidente);
            return Results.Ok(new { mensaje = "Asignado" });
        }

        private static async Task<IResult> CambiarEstado(int id, CambiarEstadoRequest request, IIncidenteRepository repo, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await repo.GetByIdAsync(id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var nuevoEstado = await db.EstadosIncidente.FindAsync(request.EstadoId);
            if (nuevoEstado == null) return Results.BadRequest(new { error = "Estado no existe" });

            incidente.EstadoId = request.EstadoId;
            // Solo se cierra el incidente si el nuevo estado es Resuelto/Cerrado (antes se cerraba en CUALQUIER cambio de estado).
            if (nuevoEstado.Nombre is "Resuelto" or "Cerrado")
                incidente.FechaCierre = DateTime.UtcNow;

            await repo.UpdateAsync(incidente);
            return Results.Ok(new { mensaje = "Estado actualizado" });
        }

        private static async Task<IResult> AgregarBitacora(int id, AgregarBitacoraRequest request, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await db.Incidentes.FindAsync(id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var bitacora = new Models.BitacoraIncidente
            {
                IncidenteId = id,
                UsuarioId = usuario.Id,
                Accion = request.Accion,
                Comentario = request.Comentario,
                Timestamp = DateTime.UtcNow
            };

            await db.BitacorasIncidente.AddAsync(bitacora);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/incidentes/{id}/bitacora", new { id = bitacora.Id });
        }

        private static async Task<IResult> ObtenerBitacora(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await db.Incidentes.FindAsync(id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var bitacora = await db.BitacorasIncidente
                .Where(b => b.IncidenteId == id)
                .OrderBy(b => b.Timestamp)
                .Select(b => new BitacoraDTO
                {
                    Id = b.Id,
                    Accion = b.Accion,
                    Comentario = b.Comentario,
                    Usuario = b.Usuario!.Nombre,
                    Timestamp = b.Timestamp
                })
                .ToListAsync();

            return bitacora.Any() ? Results.Ok(new { total = bitacora.Count, items = bitacora }) : Results.NotFound();
        }

        private static async Task<IResult> AgregarEvidencia(int id, AgregarEvidenciaRequest request, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await db.Incidentes.FindAsync(id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var evidencia = new Models.EvidenciaIncidente
            {
                IncidenteId = id,
                NombreArchivo = request.NombreArchivo,
                RutaAlmacenamiento = request.RutaAlmacenamiento,
                SubidoPorId = usuario.Id,
                Fecha = DateTime.UtcNow
            };

            await db.EvidenciasIncidente.AddAsync(evidencia);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/incidentes/{id}/evidencia", new { id = evidencia.Id });
        }

        private static async Task<IResult> ObtenerEvidencia(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await db.Incidentes.FindAsync(id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var evidencia = await db.EvidenciasIncidente
                .Where(e => e.IncidenteId == id)
                .OrderBy(e => e.Fecha)
                .Select(e => new EvidenciaDTO
                {
                    Id = e.Id,
                    NombreArchivo = e.NombreArchivo,
                    RutaAlmacenamiento = e.RutaAlmacenamiento,
                    SubidoPor = e.SubidoPor!.Nombre,
                    Fecha = e.Fecha
                })
                .ToListAsync();

            return evidencia.Any() ? Results.Ok(new { total = evidencia.Count, items = evidencia }) : Results.NotFound();
        }

        private static async Task<IResult> ConsultarCumplimientoSla(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await db.Incidentes.Include(i => i.Severidad).FirstOrDefaultAsync(i => i.Id == id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var horas = ((incidente.FechaCierre ?? DateTime.UtcNow) - incidente.FechaCreacion).TotalHours;
            var sla = (double)(incidente.Severidad?.HorasSla ?? 0);

            return Results.Ok(new { horasTranscurridas = horas, horasSLA = sla, cumple = horas <= sla });
        }

        record CrearIncidenteRequest(string Titulo, string Descripcion, int ActivoId, int SeveridadId);
        record ActualizarIncidenteRequest(string? Titulo, string? Descripcion);
        record AsignarAnalistaRequest(int AnalistaId);
        record CambiarEstadoRequest(int EstadoId);
        record AgregarBitacoraRequest(string Accion, string? Comentario);
        record AgregarEvidenciaRequest(string NombreArchivo, string RutaAlmacenamiento);
    }
}
