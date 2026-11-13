namespace SentryGuard.Models;

// Entidades sin clave mapeadas a vistas SQL (creadas en la migración AuditoriaCategoriaTransicionesVistas).
// Alimentan los reportes y el dashboard sin duplicar la lógica en C#.

/// <summary>vw_IncidenteDetalle: un incidente con sus nombres de catálogo y horas de resolución.</summary>
public class IncidenteDetalleVista
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public int OrganizacionId { get; set; }
    public string Organizacion { get; set; } = null!;
    public int ActivoId { get; set; }
    public string Activo { get; set; } = null!;
    public string CategoriaActivo { get; set; } = null!;
    public int? CategoriaIncidenteId { get; set; }
    public string? CategoriaIncidente { get; set; }
    public int SeveridadId { get; set; }
    public string Severidad { get; set; } = null!;
    public int EstadoId { get; set; }
    public string Estado { get; set; } = null!;
    public int? AnalistaAsignadoId { get; set; }
    public string? Analista { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? HorasResolucion { get; set; }
}

/// <summary>vw_CumplimientoSla: plazo SLA aplicable a cada incidente y si se cumplió.</summary>
public class CumplimientoSlaVista
{
    public int IncidenteId { get; set; }
    public int OrganizacionId { get; set; }
    public int SeveridadId { get; set; }
    public int EstadoId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public decimal HorasSla { get; set; }
    public decimal HorasTranscurridas { get; set; }
    public bool CumpleSla { get; set; }
}

/// <summary>vw_IndicadoresMensuales: totales, resueltos, tiempo promedio de resolución y SLA por mes y severidad.</summary>
public class IndicadorMensualVista
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int SeveridadId { get; set; }
    public string Severidad { get; set; } = null!;
    public int TotalIncidentes { get; set; }
    public int Resueltos { get; set; }
    public decimal? PromedioHorasResolucion { get; set; }
    public int CumplenSla { get; set; }
}
