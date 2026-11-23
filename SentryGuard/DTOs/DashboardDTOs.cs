using System;
using System.Collections.Generic;

namespace SentryGuard.DTOs
{
    /// <summary>
    /// Indicador individual del dashboard (KPI).
    /// </summary>
    public class IndicadorDTO
    {
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal? Valor { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public decimal? Variacion { get; set; }
    }

    /// <summary>
    /// Respuesta del endpoint GET /api/v1/dashboard/indicadores.
    /// </summary>
    public class IndicadoresDTO
    {
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }
        public List<IndicadorDTO> Items { get; set; } = new();
    }

    /// <summary>
    /// Punto individual de una serie de datos para gráficos.
    /// </summary>
    public class PuntoSerieDTO
    {
        public string Etiqueta { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    /// <summary>
    /// Respuesta del endpoint GET /api/v1/dashboard/series.
    /// </summary>
    public class SeriesDashboardDTO
    {
        public List<PuntoSerieDTO> IncidentesPorMes { get; set; } = new();
        public List<PuntoSerieDTO> CumplimientoSlaPorMes { get; set; } = new();
        public List<PuntoSerieDTO> PorSeveridad { get; set; } = new();
        public List<PuntoSerieDTO> PorCategoria { get; set; } = new();
        public List<PuntoSerieDTO> PorOrganizacion { get; set; } = new();
        public List<PuntoSerieDTO> CargaPorAnalista { get; set; } = new();
    }
}
