using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.DTOs;

namespace SentryGuard.Endpoints
{
    // Consulta de tb_LogAuditoria, que llena StructuredLoggingMiddleware en cada escritura exitosa.
    public static class AuditoriaAPI
    {
        public static void MapAuditoriaApi(this WebApplication app)
        {
            app.MapGroup("/api/v1/auditoria")
                .WithTags("Auditoría")
                .RequireAuthorization(p => p.RequireRole("Administrador"))
                .MapGet("", Listar).WithName("Listar auditoria");
        }

        private static async Task<IResult> Listar(SentryGuardDbContext db, int page = 1, int pageSize = 20, string? q = null, int? usuarioId = null)
        {
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);

            IQueryable<Models.LogAuditoria> query = db.LogsAuditoria;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                if (termino.Length > 100) termino = termino[..100];
                query = query.Where(l => l.Accion.Contains(termino));
            }
            if (usuarioId.HasValue) query = query.Where(l => l.UsuarioId == usuarioId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .ThenByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AuditoriaDTO
                {
                    Id = l.Id,
                    Usuario = db.Usuarios.Where(u => u.Id == l.UsuarioId).Select(u => u.Nombre).FirstOrDefault(),
                    Accion = l.Accion,
                    IpOrigen = l.IpOrigen,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize), items });
        }
    }
}
