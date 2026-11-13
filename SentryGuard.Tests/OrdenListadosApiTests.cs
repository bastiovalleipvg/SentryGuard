using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SentryGuard.Tests;

/// <summary>RF-06: orden por columna (orden=campo, orden=-campo) con lista blanca de columnas.</summary>
public class OrdenListadosApiTests : IClassFixture<SentryGuardApiFactory>
{
    private readonly SentryGuardApiFactory _factory;

    public OrdenListadosApiTests(SentryGuardApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateClient();
        client.UseBearerToken(await client.LoginAsync("admin@sentryguard.cl"));
        return client;
    }

    private static async Task CrearIncidenteAsync(HttpClient client, string titulo, int severidadId)
    {
        var respuesta = await client.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo,
            descripcion = "Descripcion suficientemente larga",
            activoId = 1,
            severidadId
        });
        respuesta.EnsureSuccessStatusCode();
    }

    // RUT aleatorio con digito verificador valido (modulo 11).
    private static string RutValido()
    {
        var cuerpo = Random.Shared.Next(10_000_000, 99_999_999).ToString();
        int suma = 0, factor = 2;
        for (var i = cuerpo.Length - 1; i >= 0; i--)
        {
            suma += (cuerpo[i] - '0') * factor;
            factor = factor == 7 ? 2 : factor + 1;
        }
        var resto = 11 - (suma % 11);
        return $"{cuerpo}-{(resto == 11 ? "0" : resto == 10 ? "K" : resto.ToString())}";
    }

    private static async Task<List<JsonElement>> ItemsAsync(HttpClient client, string url)
    {
        var json = await client.GetFromJsonAsync<JsonElement>(url);
        return json.GetProperty("items").EnumerateArray().ToList();
    }

    private static List<string> Textos(IEnumerable<JsonElement> items, string campo) =>
        items.Select(i => i.GetProperty(campo).GetString()!).ToList();

    private static void AssertOrdenado(List<string> valores, bool descendente)
    {
        for (var i = 1; i < valores.Count; i++)
        {
            var comparacion = string.Compare(valores[i - 1], valores[i], CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            Assert.True(descendente ? comparacion >= 0 : comparacion <= 0, $"'{valores[i - 1]}' y '{valores[i]}' no respetan el orden");
        }
    }

    [Fact]
    public async Task Incidentes_OrdenaPorTituloYSeveridad_AscendenteYDescendente()
    {
        var admin = await AdminAsync();
        await CrearIncidenteAsync(admin, "Aaaa primer incidente", severidadId: 1);
        await CrearIncidenteAsync(admin, "Zzzz ultimo incidente", severidadId: 4);

        var ascendente = await ItemsAsync(admin, "/api/v1/incidentes?orden=titulo&pageSize=50");
        var descendente = await ItemsAsync(admin, "/api/v1/incidentes?orden=-titulo&pageSize=50");
        AssertOrdenado(Textos(ascendente, "titulo"), descendente: false);
        AssertOrdenado(Textos(descendente, "titulo"), descendente: true);
        Assert.StartsWith("Aaaa", ascendente.First().GetProperty("titulo").GetString());
        Assert.StartsWith("Zzzz", descendente.First().GetProperty("titulo").GetString());

        var porSeveridad = await ItemsAsync(admin, "/api/v1/incidentes?orden=-severidad&pageSize=50");
        var ids = porSeveridad.Select(i => i.GetProperty("severidadId").GetInt32()).ToList();
        Assert.Equal(ids.OrderByDescending(x => x).ToList(), ids);
        Assert.Equal(4, ids.First());
    }

    [Fact]
    public async Task Incidentes_SinOrden_MantieneLaFechaMasRecientePrimero()
    {
        var admin = await AdminAsync();
        await CrearIncidenteAsync(admin, "Incidente para orden por defecto", severidadId: 2);

        var items = await ItemsAsync(admin, "/api/v1/incidentes?pageSize=50");
        var fechas = items.Select(i => DateTime.Parse(i.GetProperty("fechaCreacion").GetString()!, CultureInfo.InvariantCulture)).ToList();
        Assert.Equal(fechas.OrderByDescending(f => f).ToList(), fechas);
    }

    [Theory]
    [InlineData("/api/v1/incidentes?orden=password")]
    [InlineData("/api/v1/incidentes?orden=titulo;DROP TABLE tb_Incidente")]
    [InlineData("/api/v1/activos?orden=-xyz")]
    [InlineData("/api/v1/organizaciones?orden=hashdeclave")]
    [InlineData("/api/v1/usuarios?orden=passwordHash")]
    public async Task ColumnaDeOrdenNoPermitida_Devuelve400ConElMensajeEstandar(string url)
    {
        var admin = await AdminAsync();

        var respuesta = await admin.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Columna de orden no válida", cuerpo.GetProperty("errors").GetProperty("orden")[0].GetString());
    }

    [Theory]
    [InlineData("/api/v1/activos", "nombre", "nombre")]
    [InlineData("/api/v1/organizaciones", "razonsocial", "razonSocial")]
    [InlineData("/api/v1/usuarios", "email", "email")]
    public async Task OtrosListados_OrdenanPorColumnaPermitida(string ruta, string columna, string propiedad)
    {
        var admin = await AdminAsync();
        if (ruta.EndsWith("organizaciones"))
        {
            // La base de pruebas parte con una sola organizacion: se agregan dos para que el orden se pueda comprobar.
            foreach (var nombre in new[] { "Aaa Orden SpA", "Zzz Orden SpA" })
                (await admin.PostAsJsonAsync(ruta, new { razonSocial = nombre, rut = RutValido(), planId = 2 })).EnsureSuccessStatusCode();
        }

        var ascendente = Textos(await ItemsAsync(admin, $"{ruta}?orden={columna}&pageSize=50"), propiedad);
        var descendente = Textos(await ItemsAsync(admin, $"{ruta}?orden=-{columna}&pageSize=50"), propiedad);

        Assert.True(ascendente.Count > 1);
        AssertOrdenado(ascendente, descendente: false);
        AssertOrdenado(descendente, descendente: true);
    }
}
