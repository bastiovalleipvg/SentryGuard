using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Repositories;
using SentryGuard.Services;
using System.Linq.Expressions;
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

        private const int TituloMin = 5;
        private const int TituloMax = 250;
        private const int DescripcionMin = 10;
        private const int DescripcionMax = 4000;
        private const int BusquedaMax = 100;
        private const int ComentarioMax = 2000;

        // Solo https:// absolutas o rutas relativas simples: descarta javascript:, data:, file: y rutas del servidor.
        private static bool EsRutaEvidenciaValida(string ruta)
        {
            if (Uri.TryCreate(ruta, UriKind.Absolute, out var uri))
                return uri.Scheme == Uri.UriSchemeHttps;
            return !ruta.StartsWith('/') && !ruta.Contains("..") && ruta.All(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or '/');
        }

        // Proyeccion unica a DTO, traducida a SQL por EF Core (nunca materializa entidades con navegaciones, OWASP API3).
        private static readonly Expression<Func<Models.Incidente, IncidenteDTO>> ADto = i => new IncidenteDTO
        {
            Id = i.Id,
            Titulo = i.Titulo,
            Descripcion = i.Descripcion,
            SeveridadId = i.SeveridadId,
            Severidad = i.Severidad!.Nombre,
            EstadoId = i.EstadoId,
            Estado = i.Estado!.Nombre,
            ActivoId = i.ActivoId,
            Activo = i.Activo!.Nombre,
            OrganizacionId = i.OrganizacionId,
            AnalistaAsignadoId = i.AnalistaAsignadoId,
            AnalistaAsignado = i.AnalistaAsignado != null ? i.AnalistaAsignado.Nombre : null,
            FechaCreacion = i.FechaCreacion,
            FechaCierre = i.FechaCierre
        };

        // Validacion de servidor obligatoria aunque el frontend tambien valide: el cliente es controlable por el atacante.
        private static void ValidarTexto(Dictionary<string, string[]> errores, string campo, string? valor, int min, int max, bool requerido)
        {
            if (valor is null || (!requerido && min == 0 && valor.Trim().Length == 0))
            {
                if (requerido) errores[campo] = [$"El campo {campo} es obligatorio."];
                return;
            }

            var largo = valor.Trim().Length;
            if (largo < min || largo > max)
                errores[campo] = [$"Debe tener entre {min} y {max} caracteres."];
        }

        // Las acciones del flujo (crear, editar, cambiar estado, asignar) quedan en la bitacora del incidente
        // con el usuario del JWT, para que el historial no dependa de que alguien lo anote a mano.
        private static void RegistrarEnBitacora(SentryGuardDbContext db, int incidenteId, int usuarioId, string accion, string? comentario = null) =>
            db.BitacorasIncidente.Add(new Models.BitacoraIncidente
            {
                IncidenteId = incidenteId,
                UsuarioId = usuarioId,
                Accion = accion,
                Comentario = comentario,
                Timestamp = DateTime.UtcNow
            });

        private static async Task<IResult> CrearIncidente(CrearIncidenteRequest request, IIncidenteRepository repo, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var errores = new Dictionary<string, string[]>();
            ValidarTexto(errores, "titulo", request.Titulo, TituloMin, TituloMax, requerido: true);
            ValidarTexto(errores, "descripcion", request.Descripcion, DescripcionMin, DescripcionMax, requerido: true);
            if (!await db.Severidades.AnyAsync(s => s.Id == request.SeveridadId))
                errores["severidadId"] = ["La severidad seleccionada no existe."];

            var activo = await db.Activos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.ActivoId);
            if (activo == null)
                errores["activoId"] = ["El activo seleccionado no existe."];

            if (errores.Count > 0)
                return Results.ValidationProblem(errores);

            // El incidente pertenece a la organizacion dueña del activo (OWASP API1: nunca confiar en un id del body).
            if (usuario.Rol?.Nombre == "Cliente" && usuario.OrganizacionId != activo!.OrganizacionId)
                return Results.Forbid();

            var estado = await db.EstadosIncidente.FirstOrDefaultAsync(e => e.Nombre == "Abierto");
            var incidente = new Models.Incidente
            {
                OrganizacionId = activo!.OrganizacionId,
                ActivoId = request.ActivoId,
                Titulo = request.Titulo!.Trim(),
                Descripcion = request.Descripcion!.Trim(),
                SeveridadId = request.SeveridadId,
                EstadoId = estado?.Id ?? 1,
                FechaCreacion = DateTime.UtcNow
            };

            await repo.AddAsync(incidente);
            RegistrarEnBitacora(db, incidente.Id, usuario.Id, "Incidente creado");
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/incidentes/{incidente.Id}", new { id = incidente.Id, titulo = incidente.Titulo });
        }

        private static async Task<IResult> ListarIncidentes(SentryGuardDbContext db, HttpContext httpContext, AuthService authService,
            int page = 1, int pageSize = 10, string? q = null, int? estadoId = null, int? severidadId = null, int? activoId = null)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            (page, pageSize) = Paginacion.Normalizar(page, pageSize);

            IQueryable<Models.Incidente> query = db.Incidentes;

            // Cliente: solo su organizacion. Analista: solo lo asignado. Administrador/Supervisor: todo.
            if (usuario.Rol?.Nombre == "Cliente")
                query = query.Where(i => i.OrganizacionId == usuario.OrganizacionId);
            else if (usuario.Rol?.Nombre == "Analista")
                query = query.Where(i => i.AnalistaAsignadoId == usuario.Id);

            // EF Core parametriza el valor de la busqueda (LIKE @p), no concatena SQL.
            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                if (termino.Length > BusquedaMax) termino = termino[..BusquedaMax];
                query = query.Where(i => i.Titulo.Contains(termino));
            }
            if (estadoId.HasValue) query = query.Where(i => i.EstadoId == estadoId.Value);
            if (severidadId.HasValue) query = query.Where(i => i.SeveridadId == severidadId.Value);
            if (activoId.HasValue) query = query.Where(i => i.ActivoId == activoId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.FechaCreacion)
                .ThenByDescending(i => i.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ADto)
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize), items });
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

            var dto = await db.Incidentes.Where(i => i.Id == id).Select(ADto).FirstAsync();
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

            var errores = new Dictionary<string, string[]>();
            ValidarTexto(errores, "titulo", request.Titulo, TituloMin, TituloMax, requerido: false);
            ValidarTexto(errores, "descripcion", request.Descripcion, DescripcionMin, DescripcionMax, requerido: false);
            if (request.SeveridadId.HasValue && !await db.Severidades.AnyAsync(s => s.Id == request.SeveridadId.Value))
                errores["severidadId"] = ["La severidad seleccionada no existe."];
            if (errores.Count > 0)
                return Results.ValidationProblem(errores);

            if (request.Titulo is not null) incidente.Titulo = request.Titulo.Trim();
            if (request.Descripcion is not null) incidente.Descripcion = request.Descripcion.Trim();
            if (request.SeveridadId.HasValue) incidente.SeveridadId = request.SeveridadId.Value;

            RegistrarEnBitacora(db, incidente.Id, usuario.Id, "Datos del incidente actualizados");
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

        private static async Task<IResult> AsignarAnalista(int id, AsignarAnalistaRequest request, IIncidenteRepository repo, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await repo.GetByIdAsync(id);
            if (incidente == null) return Results.NotFound();

            var analista = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == request.AnalistaId);
            if (analista == null || analista.Rol?.Nombre != "Analista")
                return Results.BadRequest(new { error = "El usuario indicado no existe o no tiene rol Analista" });

            incidente.AnalistaAsignadoId = request.AnalistaId;
            RegistrarEnBitacora(db, incidente.Id, usuario.Id, "Analista asignado", analista.Nombre);
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
            var estadoAnterior = await db.EstadosIncidente.Where(e => e.Id == incidente.EstadoId).Select(e => e.Nombre).FirstOrDefaultAsync();

            incidente.EstadoId = request.EstadoId;
            RegistrarEnBitacora(db, incidente.Id, usuario.Id, "Cambio de estado", $"{estadoAnterior} → {nuevoEstado.Nombre}");
            // Solo se cierra el incidente si el nuevo estado es Resuelto/Cerrado (antes se cerraba en CUALQUIER cambio de estado).
            if (nuevoEstado.Nombre is "Resuelto" or "Cerrado")
                incidente.FechaCierre ??= DateTime.UtcNow;
            else
                incidente.FechaCierre = null;

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

            var errores = new Dictionary<string, string[]>();
            ValidarTexto(errores, "accion", request.Accion, 3, 100, requerido: true);
            ValidarTexto(errores, "comentario", request.Comentario, 0, ComentarioMax, requerido: false);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var bitacora = new Models.BitacoraIncidente
            {
                IncidenteId = id,
                UsuarioId = usuario.Id,
                Accion = request.Accion!.Trim(),
                Comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim(),
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
                .OrderByDescending(b => b.Timestamp).ThenByDescending(b => b.Id)
                .Select(b => new BitacoraDTO
                {
                    Id = b.Id,
                    Accion = b.Accion,
                    Comentario = b.Comentario,
                    Usuario = b.Usuario!.Nombre,
                    Timestamp = b.Timestamp
                })
                .ToListAsync();

            // Un incidente sin entradas devuelve una lista vacia (antes 404, que se confundia con "incidente inexistente").
            return Results.Ok(new { total = bitacora.Count, items = bitacora });
        }

        private static async Task<IResult> AgregarEvidencia(int id, AgregarEvidenciaRequest request, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await db.Incidentes.FindAsync(id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            var errores = new Dictionary<string, string[]>();
            ValidarTexto(errores, "nombreArchivo", request.NombreArchivo, 1, 255, requerido: true);
            ValidarTexto(errores, "rutaAlmacenamiento", request.RutaAlmacenamiento, 1, 1000, requerido: true);
            if (!errores.ContainsKey("rutaAlmacenamiento") && !EsRutaEvidenciaValida(request.RutaAlmacenamiento!.Trim()))
                errores["rutaAlmacenamiento"] = ["Usa una URL https:// o una ruta relativa del repositorio de evidencias (letras, números, punto, guion, guion bajo y /)."];
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var evidencia = new Models.EvidenciaIncidente
            {
                IncidenteId = id,
                NombreArchivo = request.NombreArchivo!.Trim(),
                RutaAlmacenamiento = request.RutaAlmacenamiento!.Trim(),
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
                .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id)
                .Select(e => new EvidenciaDTO
                {
                    Id = e.Id,
                    NombreArchivo = e.NombreArchivo,
                    RutaAlmacenamiento = e.RutaAlmacenamiento,
                    SubidoPor = e.SubidoPor!.Nombre,
                    Fecha = e.Fecha
                })
                .ToListAsync();

            return Results.Ok(new { total = evidencia.Count, items = evidencia });
        }

        private static async Task<IResult> ConsultarCumplimientoSla(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();

            var incidente = await db.Incidentes.Include(i => i.Severidad).FirstOrDefaultAsync(i => i.Id == id);
            if (incidente == null) return Results.NotFound();

            if (!authService.ValidaAccesoIncidente(usuario, incidente))
                return Results.Forbid();

            // Prioridad: horas pactadas en el contrato SLA vigente al crear el incidente; si no hay, las de la severidad.
            var horasContrato = await db.ContratosSla
                .Where(c => c.OrganizacionId == incidente.OrganizacionId
                    && c.VigenteDesde <= incidente.FechaCreacion
                    && (c.VigenteHasta == null || c.VigenteHasta >= incidente.FechaCreacion))
                .Join(db.ContratosSlaDetalle, c => c.Id, d => d.ContratoId, (c, d) => d)
                .Where(d => d.SeveridadId == incidente.SeveridadId)
                .Select(d => (decimal?)d.HorasSla)
                .FirstOrDefaultAsync();

            var horas = ((incidente.FechaCierre ?? DateTime.UtcNow) - incidente.FechaCreacion).TotalHours;
            var sla = (double)(horasContrato ?? incidente.Severidad?.HorasSla ?? 0);

            return Results.Ok(new
            {
                horasTranscurridas = Math.Round(horas, 2),
                horasSLA = sla,
                cumple = horas <= sla,
                fuente = horasContrato.HasValue ? "Contrato SLA" : "Severidad",
                fechaLimite = incidente.FechaCreacion.AddHours(sla),
                cerrado = incidente.FechaCierre.HasValue
            });
        }

        record CrearIncidenteRequest(string? Titulo, string? Descripcion, int ActivoId, int SeveridadId);
        record ActualizarIncidenteRequest(string? Titulo, string? Descripcion, int? SeveridadId);
        record AsignarAnalistaRequest(int AnalistaId);
        record CambiarEstadoRequest(int EstadoId);
        record AgregarBitacoraRequest(string? Accion, string? Comentario);
        record AgregarEvidenciaRequest(string? NombreArchivo, string? RutaAlmacenamiento);
    }
}
