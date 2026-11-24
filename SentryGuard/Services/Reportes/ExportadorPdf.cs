using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SentryGuard.DTOs;

namespace SentryGuard.Services.Reportes
{
    public class ExportadorPdf : IExportador
    {
        static ExportadorPdf()
        {
            // Licencia Community de QuestPDF (gratuita bajo 1 000 000 USD de ingresos anuales; uso academico).
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string Formato => "pdf";
        public string ContentType => "application/pdf";
        public string Extension => "pdf";

        public void Exportar(ContenidoReporte reporte, Stream destino)
        {
            var filtros = reporte.Filtros.Count == 0 ? "ninguno" : string.Join(", ", reporte.Filtros.Select(f => $"{f.Key}: {f.Value}"));
            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(t => t.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(reporte.Titulo).FontSize(16).Bold();
                        col.Item().Text($"Emitido: {DateTime.Now:dd-MM-yyyy HH:mm}  |  Filtros: {filtros}").FontSize(8);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c => { foreach (var _ in reporte.Columnas) c.RelativeColumn(); });
                            tabla.Header(h =>
                            {
                                foreach (var nombre in reporte.Columnas)
                                    h.Cell().Background("#1F2A44").Padding(3).Text(nombre).FontColor(Colors.White).Bold();
                            });
                            foreach (var fila in reporte.Filas)
                                foreach (var v in fila)
                                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(ExportadorCsv.Texto(v));
                        });

                        if (reporte.Totales.Count > 0)
                        {
                            col.Item().PaddingTop(10).Text("Totales").Bold();
                            foreach (var t in reporte.Totales)
                                col.Item().Text($"{t.Key}: {t.Value:0.##}");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(destino);
        }
    }
}
