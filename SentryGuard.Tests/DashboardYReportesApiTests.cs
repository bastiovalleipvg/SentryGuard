using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentryGuard.Data;

namespace SentryGuard.Tests;

/// <summary>Hito 4: dashboard y reportes (permisos por rol, filtros, formatos de exportacion y auditoria).</summary>
public class DashboardYReportesApiTests : IClassFixture<SentryGuardApiFactory>
{
    private readonly SentryGuardApiFactory _factory;

    public DashboardYReportesApiTests(SentryGuardApiFactory factory) => _factory = factory;

    private static readonly SemaphoreSlim Candado = new(1, 1);
    private static bool _datosCreados;

    private async Task<HttpClient> ComoAsync(string email)
    {
        var client = _factory.CreateClient();
        client.UseBearerToken(await client.LoginAsync(email));
        await AsegurarDatosAsync();
        return client;
    }

    /// <summary>La base de pruebas solo trae catalogos y usuarios: crea 8 incidentes (uno critico) una sola vez.</summary>
    private async Task AsegurarDatosAsync()
    {
        await Candado.WaitAsync();
        try
        {
            if (_datosCreados) return;
            var admin = _factory.CreateClient();
            admin.UseBearerToken(await admin.LoginAsync("admin@sentryguard.cl"));
            for (var i = 0; i < 8; i++)
            {
                var r = await admin.PostAsJsonAsync("/api/v1/incidentes", new
                {
                    titulo = $"Incidente de reporte {i}",
                    descripcion = "Descripcion suficientemente larga",
                    activoId = 1,
                    severidadId = i == 0 ? 4 : 2
                });
                r.EnsureSuccessStatusCode();
            }
            _datosCreados = true;
        }
        finally { Candado.Release(); }
    }

    [Fact]
    public async Task Dashboard_SinToken_Devuelve401()
    {
        var r = await _factory.CreateClient().GetAsync("/api/v1/dashboard/indicadores");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Dashboard_RangoInvertido_Devuelve400ConErrorPorCampo()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        var r = await admin.GetAsync("/api/v1/dashboard/indicadores?desde=2026-05-01&hasta=2026-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("errors").TryGetProperty("hasta", out _));
    }

    [Fact]
    public async Task Dashboard_Administrador_RecibeLosSeisIndicadores_YElClienteVeMenosOIgual()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        var cliente = await ComoAsync("cliente@sentryguard.cl");
        const string rango = "?desde=2020-01-01&hasta=2030-12-31";

        var a = await admin.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/indicadores" + rango);
        var c = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/indicadores" + rango);

        Assert.Equal(6, a.GetProperty("items").GetArrayLength());
        decimal Volumen(JsonElement e) => e.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("clave").GetString() == "volumen").GetProperty("valor").GetDecimal();
        Assert.True(Volumen(a) > 0);
        Assert.True(Volumen(c) <= Volumen(a));

        var series = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/series" + rango);
        Assert.Equal(0, series.GetProperty("porOrganizacion").GetArrayLength());
        Assert.Equal(0, series.GetProperty("cargaPorAnalista").GetArrayLength());
    }

    [Fact]
    public async Task Reportes_Analista_Devuelve403_YSinTokenDevuelve401()
    {
        var analista = await ComoAsync("analista@sentryguard.cl");
        Assert.Equal(HttpStatusCode.Forbidden, (await analista.GetAsync("/api/v1/reportes/operativo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await analista.GetAsync("/api/v1/reportes/operativo/exportar?formato=csv")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/v1/reportes/operativo")).StatusCode);
    }

    [Fact]
    public async Task Reporte_Operativo_PaginaYRespetaLosFiltros_YElClienteSoloVeSuOrganizacion()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        var cliente = await ComoAsync("cliente@sentryguard.cl");

        var todo = await admin.GetFromJsonAsync<JsonElement>("/api/v1/reportes/operativo?pageSize=5");
        Assert.Equal(5, todo.GetProperty("filas").GetArrayLength());
        Assert.True(todo.GetProperty("total").GetInt32() > 5);
        Assert.True(todo.GetProperty("totales").TryGetProperty("Incidentes", out _));

        var criticos = await admin.GetFromJsonAsync<JsonElement>("/api/v1/reportes/operativo?severidadId=4&pageSize=50");
        Assert.All(criticos.GetProperty("filas").EnumerateArray(), f => Assert.Equal("Crítica", f.GetProperty("Severidad").GetString()));

        var propios = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/reportes/operativo?pageSize=50");
        var orgs = propios.GetProperty("filas").EnumerateArray().Select(f => f.GetProperty("Organización").GetString()).Distinct().ToList();
        Assert.True(orgs.Count <= 1);
        Assert.True(propios.GetProperty("total").GetInt32() <= todo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Reporte_Sla_ExigeRangoDeFechas_YOrdenInvalidoDa400()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/reportes/sla")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/reportes/sla?desde=2020-01-01&hasta=2030-01-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/reportes/operativo?orden=password")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/v1/reportes/inexistente")).StatusCode);
    }

    [Fact]
    public async Task Reporte_Gerencial_Trae_TotalesPorOrganizacionSeveridadYEstado()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        var r = await admin.GetFromJsonAsync<JsonElement>("/api/v1/reportes/gerencial?pageSize=50");
        Assert.Equal(4, r.GetProperty("columnas").GetArrayLength());
        Assert.True(r.GetProperty("totales").GetProperty("Incidentes").GetDecimal() > 0);
    }

    [Fact]
    public async Task Exportar_Csv_EmpiezaConBom_UsaPuntoYComa_YDejaAuditoria()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        var r = await admin.GetAsync("/api/v1/reportes/operativo/exportar?formato=csv");
        r.EnsureSuccessStatusCode();
        Assert.StartsWith("reporte-operativo-", r.Content.Headers.ContentDisposition!.FileName);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var primera = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n")[0];
        Assert.Contains(";", primera);
        Assert.Contains("Título", primera);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SentryGuardDbContext>();
        var log = await db.LogsAuditoria.AsNoTracking().Where(l => l.Accion == "EXPORTAR Reporte").OrderByDescending(l => l.Id).FirstAsync();
        Assert.Contains("csv", log.ValoresNuevos);
        Assert.Contains("operativo", log.ValoresNuevos);
        Assert.NotNull(log.UsuarioId);
    }

    [Fact]
    public async Task Exportar_Excel_Genera_UnLibroConEncabezadosYTotales()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        var r = await admin.GetAsync("/api/v1/reportes/gerencial/exportar?formato=xlsx");
        r.EnsureSuccessStatusCode();
        Assert.Contains("spreadsheetml", r.Content.Headers.ContentType!.MediaType);
        using var ms = new MemoryStream(await r.Content.ReadAsByteArrayAsync());
        using var libro = new XLWorkbook(ms);
        var hoja = libro.Worksheet(1);
        Assert.Equal("Organización", hoja.Cell(5, 1).GetString());
        Assert.Contains(hoja.CellsUsed(), c => c.GetString() == "Totales");
    }

    [Fact]
    public async Task Exportar_Pdf_EmpiezaConPdf_YFormatoInvalidoDa400()
    {
        var admin = await ComoAsync("admin@sentryguard.cl");
        var r = await admin.GetAsync("/api/v1/reportes/gerencial/exportar?formato=pdf");
        r.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", r.Content.Headers.ContentType!.MediaType);
        Assert.Equal("%PDF", Encoding.ASCII.GetString((await r.Content.ReadAsByteArrayAsync())[..4]));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/reportes/gerencial/exportar?formato=exe")).StatusCode);
    }
}
