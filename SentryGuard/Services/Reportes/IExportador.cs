using System.IO;
using SentryGuard.DTOs;

namespace SentryGuard.Services.Reportes
{
    /// <summary>Estrategia de exportacion: cada formato escribe el mismo reporte en un Stream.</summary>
    public interface IExportador
    {
        string Formato { get; }
        string ContentType { get; }
        string Extension { get; }
        void Exportar(ContenidoReporte reporte, Stream destino);
    }
}
