using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Models;

namespace SentryGuard.Services
{
    public interface IReporteService
    {
        Task<ServiceResult<PaginaReporteDTO>> ObtenerPaginaAsync(string tipo, FiltrosReporte filtros, int page, int pageSize, string? orden, Usuario usuario, CancellationToken ct = default);
        Task<ServiceResult<ContenidoReporte>> ObtenerContenidoAsync(string tipo, FiltrosReporte filtros, Usuario usuario, CancellationToken ct = default);
        Task RegistrarExportacionAsync(string tipo, string formato, FiltrosReporte filtros, Usuario usuario, CancellationToken ct = default);
    }

    public class ReporteService : IReporteService
    {
        public const int MaxFilasExportacion = 20000;
        public static readonly string[] Tipos = ["operativo", "gerencial", "sla"];

        private static readonly IReadOnlyDictionary<string, Expression<Func<IncidenteDetalleVista, object>>> ColumnasOrden =
            new Dictionary<string, Expression<Func<IncidenteDetalleVista, object>>>
            {
                ["id"] = x => x.Id,
                ["titulo"] = x => x.Titulo,
                ["organizacion"] = x => x.Organizacion,
                ["severidad"] = x => x.SeveridadId,
                ["estado"] = x => x.EstadoId,
                ["fechacreacion"] = x => x.FechaCreacion
            };

        private readonly SentryGuardDbContext _db;

        public ReporteService(SentryGuardDbContext db) => _db = db;

        public async Task<ServiceResult<PaginaReporteDTO>> ObtenerPaginaAsync(
            string tipo, FiltrosReporte filtros, int page, int pageSize, string? orden, Usuario usuario, CancellationToken ct = default)
        {
            var previo = Validar<PaginaReporteDTO>(tipo, filtros, usuario, orden);
            if (previo != null) return previo;
            var f = Efectivos(filtros, usuario);

            ContenidoReporte contenido;
            int total;
            if (tipo == "operativo")
            {
                var consulta = ConsultaOperativa(f);
                total = await consulta.CountAsync(ct);
                var pagina = await consulta
                    .Ordenar(orden, ColumnasOrden, q => q.OrderByDescending(x => x.FechaCreacion), x => x.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
                contenido = await ArmarOperativoAsync(pagina, consulta, f, ct);
            }
            else
            {
                contenido = tipo == "gerencial" ? await ArmarGerencialAsync(f, ct) : await ArmarSlaAsync(f, ct);
                total = contenido.Filas.Count;
                contenido.Filas = contenido.Filas.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }

            var dto = new PaginaReporteDTO
            {
                Tipo = tipo,
                Filtros = contenido.Filtros,
                Totales = contenido.Totales,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Columnas = contenido.Columnas,
                Filas = contenido.Filas
                    .Select(fila => contenido.Columnas.Select((c, i) => (c, v: fila[i])).ToDictionary(p => p.c, p => p.v))
                    .ToList()
            };
            return ServiceResult<PaginaReporteDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ContenidoReporte>> ObtenerContenidoAsync(
            string tipo, FiltrosReporte filtros, Usuario usuario, CancellationToken ct = default)
        {
            var previo = Validar<ContenidoReporte>(tipo, filtros, usuario, null);
            if (previo != null) return previo;
            var f = Efectivos(filtros, usuario);

            ContenidoReporte contenido;
            if (tipo == "operativo")
            {
                var consulta = ConsultaOperativa(f);
                var filas = await consulta.OrderByDescending(x => x.FechaCreacion).ThenBy(x => x.Id).Take(MaxFilasExportacion).ToListAsync(ct);
                contenido = await ArmarOperativoAsync(filas, consulta, f, ct);
            }
            else
            {
                contenido = tipo == "gerencial" ? await ArmarGerencialAsync(f, ct) : await ArmarSlaAsync(f, ct);
            }
            return ServiceResult<ContenidoReporte>.Ok(contenido);
        }

        public async Task RegistrarExportacionAsync(string tipo, string formato, FiltrosReporte filtros, Usuario usuario, CancellationToken ct = default)
        {
            var detalle = JsonSerializer.Serialize(new { tipo, formato, filtros = Efectivos(filtros, usuario) });
            _db.LogsAuditoria.Add(new LogAuditoria
            {
                UsuarioId = usuario.Id,
                Accion = "EXPORTAR Reporte",
                Entidad = "Reporte",
                ValoresNuevos = detalle,
                IpOrigen = "API",
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        // ---------- validaciones y permisos ----------

        private static ServiceResult<T>? Validar<T>(string tipo, FiltrosReporte filtros, Usuario usuario, string? orden)
        {
            var rol = usuario.Rol?.Nombre ?? string.Empty;
            if (rol == "Analista" || (rol != "Administrador" && rol != "Supervisor" && rol != "Cliente"))
                return ServiceResult<T>.Forbid("Tu rol no tiene acceso a los reportes.");

            var errores = new Dictionary<string, string[]>();
            if (!Tipos.Contains(tipo))
                return ServiceResult<T>.NotFound($"El reporte '{tipo}' no existe. Usa: {string.Join(", ", Tipos)}.", "REPORTE_NO_ENCONTRADO");
            if (filtros.Desde.HasValue && filtros.Hasta.HasValue && filtros.Hasta < filtros.Desde)
                errores["hasta"] = ["La fecha 'hasta' debe ser mayor o igual que 'desde'."];
            if (tipo == "sla")
            {
                if (!filtros.Desde.HasValue) errores["desde"] = ["El reporte de SLA exige un rango de fechas."];
                if (!filtros.Hasta.HasValue) errores["hasta"] = ["El reporte de SLA exige un rango de fechas."];
            }
            var errorOrden = tipo == "operativo" ? Ordenamiento.Validar(orden, ColumnasOrden) : null;
            if (errorOrden != null) foreach (var e in errorOrden) errores[e.Key] = e.Value;

            return errores.Count > 0 ? ServiceResult<T>.Validation(errores, "Los filtros del reporte no son válidos.", "FILTROS_INVALIDOS") : null;
        }

        private static FiltrosReporte Efectivos(FiltrosReporte f, Usuario usuario) =>
            usuario.Rol?.Nombre == "Cliente" ? f with { OrganizacionId = usuario.OrganizacionId } : f;

        // ---------- consultas ----------

        private IQueryable<IncidenteDetalleVista> ConsultaOperativa(FiltrosReporte f)
        {
            var q = _db.VwIncidenteDetalle.AsNoTracking().AsQueryable();
            if (f.Desde.HasValue) { var d = f.Desde.Value.ToDateTime(TimeOnly.MinValue); q = q.Where(x => x.FechaCreacion >= d); }
            if (f.Hasta.HasValue) { var h = f.Hasta.Value.ToDateTime(TimeOnly.MaxValue); q = q.Where(x => x.FechaCreacion <= h); }
            if (f.OrganizacionId.HasValue) q = q.Where(x => x.OrganizacionId == f.OrganizacionId.Value);
            if (f.SeveridadId.HasValue) q = q.Where(x => x.SeveridadId == f.SeveridadId.Value);
            if (f.EstadoId.HasValue) q = q.Where(x => x.EstadoId == f.EstadoId.Value);
            if (f.CategoriaId.HasValue) q = q.Where(x => x.CategoriaIncidenteId == f.CategoriaId.Value);
            return q;
        }

        private async Task<ContenidoReporte> ArmarOperativoAsync(List<IncidenteDetalleVista> filas, IQueryable<IncidenteDetalleVista> consulta, FiltrosReporte f, CancellationToken ct)
        {
            var total = await consulta.CountAsync(ct);
            var abiertos = await consulta.CountAsync(x => x.EstadoId != 4 && x.EstadoId != 5, ct);
            var horas = await consulta.Where(x => x.HorasResolucion != null).Select(x => x.HorasResolucion!.Value).ToListAsync(ct);

            return new ContenidoReporte
            {
                Tipo = "operativo",
                Titulo = "Reporte operativo de incidentes",
                Filtros = TextoFiltros(f),
                Columnas = ["Id", "Título", "Organización", "Activo", "Categoría", "Severidad", "Estado", "Analista", "Fecha creación", "Fecha cierre", "Horas resolución"],
                Filas = filas.Select(x => new List<object?>
                {
                    x.Id, x.Titulo, x.Organizacion, x.Activo, x.CategoriaIncidente ?? "Sin categoría", x.Severidad, x.Estado,
                    x.Analista ?? "Sin asignar", x.FechaCreacion, x.FechaCierre, x.HorasResolucion
                }).ToList(),
                Totales = new Dictionary<string, decimal>
                {
                    ["Incidentes"] = total,
                    ["Abiertos"] = abiertos,
                    ["Horas promedio de resolución"] = horas.Count > 0 ? Math.Round(horas.Average(), 2) : 0m
                }
            };
        }

        private async Task<ContenidoReporte> ArmarGerencialAsync(FiltrosReporte f, CancellationToken ct)
        {
            var grupos = await ConsultaOperativa(f)
                .GroupBy(x => new { x.Organizacion, x.SeveridadId, x.Severidad, x.EstadoId, x.Estado })
                .Select(g => new { g.Key.Organizacion, g.Key.SeveridadId, g.Key.Severidad, g.Key.EstadoId, g.Key.Estado, Cantidad = g.Count() })
                .OrderBy(g => g.Organizacion).ThenBy(g => g.SeveridadId).ThenBy(g => g.EstadoId)
                .ToListAsync(ct);

            return new ContenidoReporte
            {
                Tipo = "gerencial",
                Titulo = "Reporte gerencial: incidentes por organización, severidad y estado",
                Filtros = TextoFiltros(f),
                Columnas = ["Organización", "Severidad", "Estado", "Cantidad"],
                Filas = grupos.Select(g => new List<object?> { g.Organizacion, g.Severidad, g.Estado, g.Cantidad }).ToList(),
                Totales = new Dictionary<string, decimal> { ["Incidentes"] = grupos.Sum(g => g.Cantidad) }
            };
        }

        private async Task<ContenidoReporte> ArmarSlaAsync(FiltrosReporte f, CancellationToken ct)
        {
            var detalle = ConsultaOperativa(f).Where(x => x.EstadoId == 4 || x.EstadoId == 5);
            var cerrados = await detalle
                .Join(_db.VwCumplimientoSla.AsNoTracking(), d => d.Id, s => s.IncidenteId,
                    (d, s) => new { d.FechaCreacion, d.HorasResolucion, s.CumpleSla })
                .ToListAsync(ct);

            var filas = cerrados
                .GroupBy(x => x.FechaCreacion.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var cumplen = g.Count(x => x.CumpleSla);
                    var conHoras = g.Where(x => x.HorasResolucion.HasValue).ToList();
                    return new List<object?>
                    {
                        g.Key, g.Count(), cumplen,
                        Math.Round(cumplen * 100m / g.Count(), 2),
                        conHoras.Count > 0 ? Math.Round(conHoras.Average(x => x.HorasResolucion!.Value), 2) : null
                    };
                }).ToList();

            var totalCerrados = cerrados.Count;
            var totalCumplen = cerrados.Count(x => x.CumpleSla);
            return new ContenidoReporte
            {
                Tipo = "sla",
                Titulo = "Reporte de cumplimiento del SLA por mes",
                Filtros = TextoFiltros(f),
                Columnas = ["Mes", "Cerrados", "Cumplen SLA", "% Cumplimiento", "Horas promedio de resolución"],
                Filas = filas,
                Totales = new Dictionary<string, decimal>
                {
                    ["Cerrados"] = totalCerrados,
                    ["Cumplen SLA"] = totalCumplen,
                    ["% Cumplimiento"] = totalCerrados > 0 ? Math.Round(totalCumplen * 100m / totalCerrados, 2) : 0m
                }
            };
        }

        private static Dictionary<string, string> TextoFiltros(FiltrosReporte f)
        {
            var d = new Dictionary<string, string>();
            if (f.Desde.HasValue) d["desde"] = f.Desde.Value.ToString("yyyy-MM-dd");
            if (f.Hasta.HasValue) d["hasta"] = f.Hasta.Value.ToString("yyyy-MM-dd");
            if (f.OrganizacionId.HasValue) d["organizacionId"] = f.OrganizacionId.Value.ToString();
            if (f.SeveridadId.HasValue) d["severidadId"] = f.SeveridadId.Value.ToString();
            if (f.EstadoId.HasValue) d["estadoId"] = f.EstadoId.Value.ToString();
            if (f.CategoriaId.HasValue) d["categoriaId"] = f.CategoriaId.Value.ToString();
            return d;
        }
    }
}
