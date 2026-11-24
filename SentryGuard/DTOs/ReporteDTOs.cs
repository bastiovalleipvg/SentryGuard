using System;
using System.Collections.Generic;

namespace SentryGuard.DTOs
{
    /// <summary>Filtros comunes de los reportes y de su exportacion.</summary>
    public record FiltrosReporte(DateOnly? Desde, DateOnly? Hasta, int? OrganizacionId, int? SeveridadId, int? EstadoId, int? CategoriaId);

    /// <summary>Contenido completo de un reporte (sin paginar), listo para exportar.</summary>
    public class ContenidoReporte
    {
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public Dictionary<string, string> Filtros { get; set; } = new();
        public List<string> Columnas { get; set; } = new();
        public List<List<object?>> Filas { get; set; } = new();
        public Dictionary<string, decimal> Totales { get; set; } = new();
    }

    /// <summary>Respuesta de GET /api/v1/reportes/{tipo} (esquema PaginaReporte del contrato).</summary>
    public class PaginaReporteDTO
    {
        public string Tipo { get; set; } = string.Empty;
        public Dictionary<string, string> Filtros { get; set; } = new();
        public Dictionary<string, decimal> Totales { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<string> Columnas { get; set; } = new();
        public List<Dictionary<string, object?>> Filas { get; set; } = new();
    }
}
