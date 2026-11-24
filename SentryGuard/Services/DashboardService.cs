using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Models;

namespace SentryGuard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly SentryGuardDbContext _db;
        private readonly AuthService _authService;

        public DashboardService(SentryGuardDbContext db, AuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        public async Task<ServiceResult<IndicadoresDTO>> ObtenerIndicadoresAsync(
            DateOnly? desde,
            DateOnly? hasta,
            int? organizacionId,
            int? severidadId,
            int? categoriaId,
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            if (desde.HasValue && hasta.HasValue && hasta.Value < desde.Value)
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["hasta"] = new[] { "La fecha 'hasta' debe ser mayor o igual que 'desde'." }
                };
                return ServiceResult<IndicadoresDTO>.Validation(errors, "Rango de fechas inválido.", "FECHAS_INVALIDAS");
            }

            var fechaFin = hasta ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var fechaInicio = desde ?? fechaFin.AddMonths(-12);

            string rol = usuario.Rol?.Nombre ?? string.Empty;
            int? orgIdEfectivo = rol == "Cliente" ? usuario.OrganizacionId : organizacionId;
            int? analistaIdEfectivo = rol == "Analista" ? usuario.Id : null;

            // Período actual
            var metricasActual = await CalcularMetricasPeriodoAsync(
                fechaInicio, fechaFin, orgIdEfectivo, severidadId, categoriaId, analistaIdEfectivo, cancellationToken);

            // Período anterior de igual duración
            int duracionDias = fechaFin.DayNumber - fechaInicio.DayNumber + 1;
            var fechaFinAnt = fechaInicio.AddDays(-1);
            var fechaInicioAnt = fechaFinAnt.AddDays(-duracionDias + 1);

            var metricasAnt = await CalcularMetricasPeriodoAsync(
                fechaInicioAnt, fechaFinAnt, orgIdEfectivo, severidadId, categoriaId, analistaIdEfectivo, cancellationToken);

            var items = new List<IndicadorDTO>
            {
                new()
                {
                    Clave = "volumen",
                    Nombre = "Volumen de incidentes",
                    Valor = metricasActual.Volumen,
                    Unidad = "cantidad",
                    Formula = "cantidad de incidentes creados en el período",
                    Variacion = CalcularVariacion(metricasActual.Volumen, metricasAnt.Volumen)
                },
                new()
                {
                    Clave = "cumplimientoSla",
                    Nombre = "Cumplimiento del SLA",
                    Valor = metricasActual.CumplimientoSla,
                    Unidad = "porcentaje",
                    Formula = "cerrados dentro del plazo / cerrados × 100",
                    Variacion = CalcularVariacion(metricasActual.CumplimientoSla, metricasAnt.CumplimientoSla)
                },
                new()
                {
                    Clave = "tiempoMedioResolucion",
                    Nombre = "Tiempo medio de resolución",
                    Valor = metricasActual.TiempoMedioResolucion,
                    Unidad = "horas",
                    Formula = "promedio de horas entre la creación y el cierre, de los cerrados",
                    Variacion = CalcularVariacion(metricasActual.TiempoMedioResolucion, metricasAnt.TiempoMedioResolucion)
                },
                new()
                {
                    Clave = "tiempoMedioAsignacion",
                    Nombre = "Tiempo medio de asignación",
                    Valor = metricasActual.TiempoMedioAsignacion,
                    Unidad = "horas",
                    Formula = "promedio de horas entre la creación y la entrada «Analista asignado» de tb_BitacoraIncidente",
                    Variacion = CalcularVariacion(metricasActual.TiempoMedioAsignacion, metricasAnt.TiempoMedioAsignacion)
                },
                new()
                {
                    Clave = "backlog",
                    Nombre = "Backlog activo",
                    Valor = metricasActual.Backlog,
                    Unidad = "cantidad",
                    Formula = "incidentes no Resueltos ni Cerrados al final del período",
                    Variacion = CalcularVariacion(metricasActual.Backlog, metricasAnt.Backlog)
                },
                new()
                {
                    Clave = "proporcionCriticos",
                    Nombre = "Proporción de incidentes críticos",
                    Valor = metricasActual.ProporcionCriticos,
                    Unidad = "porcentaje",
                    Formula = "críticos / total × 100",
                    Variacion = CalcularVariacion(metricasActual.ProporcionCriticos, metricasAnt.ProporcionCriticos)
                }
            };

            return ServiceResult<IndicadoresDTO>.Ok(new IndicadoresDTO
            {
                Desde = fechaInicio,
                Hasta = fechaFin,
                Items = items
            });
        }

        public async Task<ServiceResult<SeriesDashboardDTO>> ObtenerSeriesAsync(
            DateOnly? desde,
            DateOnly? hasta,
            int? organizacionId,
            int? severidadId,
            int? categoriaId,
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            if (desde.HasValue && hasta.HasValue && hasta.Value < desde.Value)
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["hasta"] = new[] { "La fecha 'hasta' debe ser mayor o igual que 'desde'." }
                };
                return ServiceResult<SeriesDashboardDTO>.Validation(errors, "Rango de fechas inválido.", "FECHAS_INVALIDAS");
            }

            var fechaFin = hasta ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var fechaInicio = desde ?? fechaFin.AddMonths(-12);

            var dtInicio = fechaInicio.ToDateTime(TimeOnly.MinValue);
            var dtFin = fechaFin.ToDateTime(TimeOnly.MaxValue);

            string rol = usuario.Rol?.Nombre ?? string.Empty;
            int? orgIdEfectivo = rol == "Cliente" ? usuario.OrganizacionId : organizacionId;
            int? analistaIdEfectivo = rol == "Analista" ? usuario.Id : null;

            var queryInc = _db.VwIncidenteDetalle.AsNoTracking()
                .Where(x => x.FechaCreacion >= dtInicio && x.FechaCreacion <= dtFin);

            if (orgIdEfectivo.HasValue)
                queryInc = queryInc.Where(x => x.OrganizacionId == orgIdEfectivo.Value);

            if (severidadId.HasValue)
                queryInc = queryInc.Where(x => x.SeveridadId == severidadId.Value);

            if (categoriaId.HasValue)
                queryInc = queryInc.Where(x => x.CategoriaIncidenteId == categoriaId.Value);

            if (analistaIdEfectivo.HasValue)
                queryInc = queryInc.Where(x => x.AnalistaAsignadoId == analistaIdEfectivo.Value);

            var incidentes = await queryInc.ToListAsync(cancellationToken);

            // 1. Incidentes por mes (YYYY-MM)
            var incidentesPorMes = incidentes
                .GroupBy(x => x.FechaCreacion.ToString("yyyy-MM"))
                .Select(g => new PuntoSerieDTO { Etiqueta = g.Key, Valor = g.Count() })
                .OrderBy(x => x.Etiqueta)
                .ToList();

            // 2. Cumplimiento SLA por mes
            var querySla = _db.VwCumplimientoSla.AsNoTracking()
                .Where(x => x.FechaCreacion >= dtInicio && x.FechaCreacion <= dtFin);

            if (orgIdEfectivo.HasValue)
                querySla = querySla.Where(x => x.OrganizacionId == orgIdEfectivo.Value);

            if (severidadId.HasValue)
                querySla = querySla.Where(x => x.SeveridadId == severidadId.Value);

            var slas = await querySla.ToListAsync(cancellationToken);

            var cumplimientoPorMes = slas
                .Where(s => s.EstadoId == 4 || s.EstadoId == 5)
                .GroupBy(x => x.FechaCreacion.ToString("yyyy-MM"))
                .Select(g => new PuntoSerieDTO
                {
                    Etiqueta = g.Key,
                    Valor = g.Any() ? Math.Round((decimal)g.Count(s => s.CumpleSla) / g.Count() * 100m, 2) : 0m
                })
                .OrderBy(x => x.Etiqueta)
                .ToList();

            // 3. Por Severidad
            var porSeveridad = incidentes
                .GroupBy(x => x.Severidad)
                .Select(g => new PuntoSerieDTO { Etiqueta = g.Key ?? "Sin severidad", Valor = g.Count() })
                .ToList();

            // 4. Por Categoría
            var porCategoria = incidentes
                .GroupBy(x => x.CategoriaIncidente ?? "Sin categoría")
                .Select(g => new PuntoSerieDTO { Etiqueta = g.Key, Valor = g.Count() })
                .ToList();

            // 5. Por Organización (vacía si es Cliente)
            var porOrganizacion = new List<PuntoSerieDTO>();
            if (rol != "Cliente")
            {
                porOrganizacion = incidentes
                    .GroupBy(x => x.Organizacion)
                    .Select(g => new PuntoSerieDTO { Etiqueta = g.Key ?? "Sin organización", Valor = g.Count() })
                    .ToList();
            }

            // 6. Carga por Analista (vacía si es Cliente o Analista; solo incidentes no resueltos/cerrados)
            var cargaPorAnalista = new List<PuntoSerieDTO>();
            if (rol != "Cliente" && rol != "Analista")
            {
                cargaPorAnalista = incidentes
                    .Where(x => x.EstadoId != 4 && x.EstadoId != 5)
                    .GroupBy(x => x.Analista ?? "Sin asignar")
                    .Select(g => new PuntoSerieDTO { Etiqueta = g.Key, Valor = g.Count() })
                    .ToList();
            }

            return ServiceResult<SeriesDashboardDTO>.Ok(new SeriesDashboardDTO
            {
                IncidentesPorMes = incidentesPorMes,
                CumplimientoSlaPorMes = cumplimientoPorMes,
                PorSeveridad = porSeveridad,
                PorCategoria = porCategoria,
                PorOrganizacion = porOrganizacion,
                CargaPorAnalista = cargaPorAnalista
            });
        }

        private async Task<(decimal Volumen, decimal? CumplimientoSla, decimal? TiempoMedioResolucion, decimal? TiempoMedioAsignacion, decimal Backlog, decimal? ProporcionCriticos)>
            CalcularMetricasPeriodoAsync(
                DateOnly inicio,
                DateOnly fin,
                int? orgId,
                int? sevId,
                int? catId,
                int? analistaId,
                CancellationToken ct)
        {
            var dtInicio = inicio.ToDateTime(TimeOnly.MinValue);
            var dtFin = fin.ToDateTime(TimeOnly.MaxValue);

            var queryInc = _db.VwIncidenteDetalle.AsNoTracking()
                .Where(x => x.FechaCreacion >= dtInicio && x.FechaCreacion <= dtFin);

            if (orgId.HasValue)
                queryInc = queryInc.Where(x => x.OrganizacionId == orgId.Value);

            if (sevId.HasValue)
                queryInc = queryInc.Where(x => x.SeveridadId == sevId.Value);

            if (catId.HasValue)
                queryInc = queryInc.Where(x => x.CategoriaIncidenteId == catId.Value);

            if (analistaId.HasValue)
                queryInc = queryInc.Where(x => x.AnalistaAsignadoId == analistaId.Value);

            var incidentes = await queryInc.ToListAsync(ct);
            decimal volumen = incidentes.Count;

            // Cumplimiento SLA de los cerrados en el período
            var querySla = _db.VwCumplimientoSla.AsNoTracking()
                .Where(x => x.FechaCreacion >= dtInicio && x.FechaCreacion <= dtFin && (x.EstadoId == 4 || x.EstadoId == 5));

            if (orgId.HasValue)
                querySla = querySla.Where(x => x.OrganizacionId == orgId.Value);

            if (sevId.HasValue)
                querySla = querySla.Where(x => x.SeveridadId == sevId.Value);

            var slas = await querySla.ToListAsync(ct);
            decimal? cumplSla = slas.Count > 0 ? Math.Round((decimal)slas.Count(s => s.CumpleSla) / slas.Count * 100m, 2) : null;

            // Tiempo medio de resolución (de los cerrados con horas)
            var conHoras = incidentes.Where(x => x.HorasResolucion.HasValue).ToList();
            decimal? tmr = conHoras.Count > 0 ? Math.Round(conHoras.Average(x => x.HorasResolucion!.Value), 2) : null;

            // Tiempo medio de asignación (horas entre creación y primera entrada 'Analista asignado')
            var incIds = incidentes.Select(x => x.Id).ToList();
            var primeraAsignacion = await _db.BitacorasIncidente.AsNoTracking()
                .Where(b => incIds.Contains(b.IncidenteId) && b.Accion == "Analista asignado")
                .GroupBy(b => b.IncidenteId)
                .Select(g => new { IncidenteId = g.Key, Timestamp = g.Min(b => b.Timestamp) })
                .ToListAsync(ct);

            var tiemposAsig = incidentes
                .Join(primeraAsignacion, i => i.Id, a => a.IncidenteId, (i, a) => (decimal)(a.Timestamp - i.FechaCreacion).TotalHours)
                .Where(h => h >= 0)
                .ToList();

            decimal? tma = tiemposAsig.Count > 0 ? Math.Round(tiemposAsig.Average(), 2) : null;

            // Backlog (incidentes creados hasta dtFin no resueltos ni cerrados a esa fecha)
            var queryBacklog = _db.VwIncidenteDetalle.AsNoTracking()
                .Where(x => x.FechaCreacion <= dtFin && (x.FechaCierre == null || x.FechaCierre > dtFin) && x.EstadoId != 4 && x.EstadoId != 5);

            if (orgId.HasValue)
                queryBacklog = queryBacklog.Where(x => x.OrganizacionId == orgId.Value);

            if (sevId.HasValue)
                queryBacklog = queryBacklog.Where(x => x.SeveridadId == sevId.Value);

            if (catId.HasValue)
                queryBacklog = queryBacklog.Where(x => x.CategoriaIncidenteId == catId.Value);

            if (analistaId.HasValue)
                queryBacklog = queryBacklog.Where(x => x.AnalistaAsignadoId == analistaId.Value);

            decimal backlog = await queryBacklog.CountAsync(ct);

            // Proporción de incidentes críticos
            int criticos = incidentes.Count(x => x.SeveridadId == 4 || x.Severidad.Equals("Crítica", StringComparison.OrdinalIgnoreCase) || x.Severidad.Equals("Critica", StringComparison.OrdinalIgnoreCase));
            decimal? propCriticos = volumen > 0 ? Math.Round((decimal)criticos / volumen * 100m, 2) : 0m;

            return (volumen, cumplSla, tmr, tma, backlog, propCriticos);
        }

        private static decimal? CalcularVariacion(decimal? actual, decimal? anterior)
        {
            if (!actual.HasValue || !anterior.HasValue) return null;
            return Math.Round(actual.Value - anterior.Value, 2);
        }
    }
}
