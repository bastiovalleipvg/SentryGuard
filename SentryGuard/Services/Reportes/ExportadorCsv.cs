using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SentryGuard.DTOs;

namespace SentryGuard.Services.Reportes
{
    /// <summary>CSV en UTF-8 con BOM y separador ';' (Excel en español lo abre bien).</summary>
    public class ExportadorCsv : IExportador
    {
        public string Formato => "csv";
        public string ContentType => "text/csv";
        public string Extension => "csv";

        public void Exportar(ContenidoReporte reporte, Stream destino)
        {
            using var w = new StreamWriter(destino, new UTF8Encoding(true), 1024, leaveOpen: true);
            w.NewLine = "\r\n";
            w.WriteLine(string.Join(';', reporte.Columnas.Select(Escapar)));
            foreach (var fila in reporte.Filas)
                w.WriteLine(string.Join(';', fila.Select(v => Escapar(Texto(v)))));
        }

        internal static string Texto(object? v) => v switch
        {
            null => "",
            DateTime d => d.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
            decimal m => m.ToString("0.##", CultureInfo.InvariantCulture),
            double m => m.ToString("0.##", CultureInfo.InvariantCulture),
            _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
        };

        private static string Escapar(string s)
        {
            // Mitiga la inyeccion de formulas en Excel (texto que empieza con = + - @)
            if (s.Length > 0 && "=+-@".Contains(s[0]) && !decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                s = "'" + s;
            return s.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
