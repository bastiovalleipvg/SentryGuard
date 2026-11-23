using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using SentryGuard.DTOs;

namespace SentryGuard.Services.Reportes
{
    public class ExportadorExcel : IExportador
    {
        public string Formato => "xlsx";
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public string Extension => "xlsx";

        public void Exportar(ContenidoReporte reporte, Stream destino)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add(reporte.Tipo);
            hoja.Cell(1, 1).Value = reporte.Titulo;
            hoja.Cell(1, 1).Style.Font.Bold = true;
            hoja.Cell(1, 1).Style.Font.FontSize = 14;
            hoja.Cell(2, 1).Value = "Emitido: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm");
            hoja.Cell(3, 1).Value = "Filtros: " + (reporte.Filtros.Count == 0 ? "ninguno" : string.Join(", ", reporte.Filtros.Select(f => $"{f.Key}={f.Value}")));

            const int filaEnc = 5;
            for (int c = 0; c < reporte.Columnas.Count; c++)
            {
                var celda = hoja.Cell(filaEnc, c + 1);
                celda.Value = reporte.Columnas[c];
                celda.Style.Font.Bold = true;
                celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2A44");
                celda.Style.Font.FontColor = XLColor.White;
            }

            int fila = filaEnc;
            foreach (var datos in reporte.Filas)
            {
                fila++;
                for (int c = 0; c < datos.Count; c++)
                {
                    var celda = hoja.Cell(fila, c + 1);
                    switch (datos[c])
                    {
                        case null: break;
                        case DateTime d: celda.Value = d; celda.Style.DateFormat.Format = "dd-MM-yyyy HH:mm"; break;
                        case decimal m: celda.Value = m; celda.Style.NumberFormat.Format = "#,##0.##"; break;
                        case int i: celda.Value = i; break;
                        case long l: celda.Value = l; break;
                        default: celda.Value = datos[c]!.ToString(); break;
                    }
                }
            }

            if (reporte.Totales.Count > 0)
            {
                fila += 2;
                hoja.Cell(fila, 1).Value = "Totales";
                hoja.Cell(fila, 1).Style.Font.Bold = true;
                foreach (var t in reporte.Totales)
                {
                    fila++;
                    hoja.Cell(fila, 1).Value = t.Key;
                    hoja.Cell(fila, 2).Value = t.Value;
                    hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.##";
                }
            }

            hoja.SheetView.FreezeRows(filaEnc);
            hoja.Columns().AdjustToContents(filaEnc, filaEnc + Math.Min(reporte.Filas.Count, 200));
            libro.SaveAs(destino);
        }
    }
}
