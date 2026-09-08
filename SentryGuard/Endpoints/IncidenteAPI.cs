using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Repositories;
using System.Security.Claims;

namespace SentryGuard.Endpoints
{
    public static class IncidenteAPI
    {
        public static void MapIncidenteApi(this WebApplication app)
        {
            var incidentes = app.MapGroup("/api/incidentes")
                .WithTags("Incidentes")
                .RequireAuthorization();

            incidentes.MapPost("", CrearIncidente).WithName("Crear incidente");
            incidentes.MapGet("", ListarIncidentes).WithName("Listar incidentes");
            incidentes.MapGet("/{id}", ObtenerIncidente).WithName("Obtener incidente");
            incidentes.MapPut("/{id}", ActualizarIncidente).WithName("Actualizar incidente");
            incidentes.MapDelete("/{id}", EliminarIncidente).WithName("Eliminar incidente");
            incidentes.MapPost("/{id}/asignar", AsignarAnalista).WithName("Asignar analista");
            incidentes.MapPost("/{id}/cambiar-estado", CambiarEstado).WithName("Cambiar estado");
            incidentes.MapPost("/{id}/bitacora", AgregarBitacora).WithName("Agregar bitácora");
            incidentes.MapGet("/{id}/bitacora", ObtenerBitacora).WithName("Obtener bitácora");
            incidentes.MapPost("/{id}/evidencia", AgregarEvidencia).WithName("Agregar evidencia");
            incidentes.MapGet("/{id}/evidencia", ObtenerEvidencia).WithName("Obtener evidencia");
            incidentes.MapGet("/sla/cumplimiento/{id}", ConsultarCumplimientoSla).WithName("Cumplimiento SLA");
        }

        private static async Task<IResult> CrearIncidente(CrearIncidenteRequest request, IIncidenteRepository repo, SentryGuardDbContext db)
        {
            var organizacionId = int.Parse("1"); // Por defecto
            var activo = await db.Activos.FindAsync(request.ActivoId);
            if (activo == null) return Results.BadRequest(new { error = "Activo no existe" });

            var estado = await db.EstadosIncidente.FirstOrDefaultAsync(e => e.Nombre == "Abierto");
            var incidente = new Models.Incidente
            {
                OrganizacionId = organizacionId,
                ActivoId = request.ActivoId,
                Titulo = request.Titulo,
                Descripcion = request.Descripcion,
                SeveridadId = request.SeveridadId,
                EstadoId = estado?.Id ?? 1,
                FechaCreacion = DateTime.UtcNow
            };

            await repo.AddAsync(incidente);
            return Results.Created($"/api/incidentes/{incidente.Id}", new { id = incidente.Id, titulo = incidente.Titulo });
        }

        private static async Task<IResult> ListarIncidentes(IIncidenteRepository repo, int page = 1, int pageSize = 10)
        {
            var incidentes = await repo.GetAllAsync();
            var total = incidentes.Count();
            var items = incidentes.Skip((page - 1) * pageSize).Take(pageSize);

            return Results.Ok(new { total, page, pageSize, totalPages = Math.Ceiling((double)total / pageSize), items });
        }

        private static async Task<IResult> ObtenerIncidente(int id, IIncidenteRepository repo)
        {
            var incidente = await repo.GetWithDetallesAsync(id);
            return incidente == null ? Results.NotFound() : Results.Ok(incidente);
        }

        private static async Task<IResult> ActualizarIncidente(int id, ActualizarIncidenteRequest request, IIncidenteRepository repo)
        {
            var incidente = await repo.GetByIdAsync(id);
            if (incidente == null) return Results.NotFound();

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

            var analista = await db.Usuarios.FindAsync(request.AnalistaId);
            if (analista == null) return Results.BadRequest(new { error = "Analista no existe" });

            incidente.AnalistaAsignadoId = request.AnalistaId;
            await repo.UpdateAsync(incidente);
            return Results.Ok(new { mensaje = "Asignado" });
        }

        private static async Task<IResult> CambiarEstado(int id, CambiarEstadoRequest request, IIncidenteRepository repo)
        {
            var incidente = await repo.GetByIdAsync(id);
            if (incidente == null) return Results.NotFound();

            incidente.EstadoId = request.EstadoId;
            incidente.FechaCierre = DateTime.UtcNow;

            await repo.UpdateAsync(incidente);
            return Results.Ok(new { mensaje = "Estado actualizado" });
        }

        private static async Task<IResult> AgregarBitacora(int id, AgregarBitacoraRequest request, SentryGuardDbContext db)
        {
            var bitacora = new Models.BitacoraIncidente
            {
                IncidenteId = id,
                UsuarioId = 1,
                Accion = request.Accion,
                Comentario = request.Comentario,
                Timestamp = DateTime.UtcNow
            };

            await db.BitacorasIncidente.AddAsync(bitacora);
            await db.SaveChangesAsync();
            return Results.Created($"/api/incidentes/{id}/bitacora", new { id = bitacora.Id });
        }

        private static async Task<IResult> ObtenerBitacora(int id, SentryGuardDbContext db)
        {
            var bitacora = await db.BitacorasIncidente.Where(b => b.IncidenteId == id).ToListAsync();
            return bitacora.Any() ? Results.Ok(new { total = bitacora.Count(), items = bitacora }) : Results.NotFound();
        }

        private static async Task<IResult> AgregarEvidencia(int id, AgregarEvidenciaRequest request, SentryGuardDbContext db)
        {
            var evidencia = new Models.EvidenciaIncidente
            {
                IncidenteId = id,
                NombreArchivo = request.NombreArchivo,
                RutaAlmacenamiento = request.RutaAlmacenamiento,
                SubidoPorId = 1,
                Fecha = DateTime.UtcNow
            };

            await db.EvidenciasIncidente.AddAsync(evidencia);
            await db.SaveChangesAsync();
            return Results.Created($"/api/incidentes/{id}/evidencia", new { id = evidencia.Id });
        }

        private static async Task<IResult> ObtenerEvidencia(int id, SentryGuardDbContext db)
        {
            var evidencia = await db.EvidenciasIncidente.Where(e => e.IncidenteId == id).ToListAsync();
            return evidencia.Any() ? Results.Ok(new { total = evidencia.Count(), items = evidencia }) : Results.NotFound();
        }

        private static async Task<IResult> ConsultarCumplimientoSla(int id, SentryGuardDbContext db)
        {
            var incidente = await db.Incidentes.Include(i => i.Severidad).FirstOrDefaultAsync(i => i.Id == id);
            if (incidente == null) return Results.NotFound();

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
