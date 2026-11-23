using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Services;

namespace SentryGuard.Tests;

/// <summary>
/// Integración de IA (RF-23, RF-24, RNF-10): endpoints con el servicio simulado, validación de la entrada,
/// roles, errores del proveedor en formato estándar y el adaptador real contra un servidor HTTP simulado.
/// </summary>
public class IaApiTests : IClassFixture<SentryGuardApiFactory>
{
    private readonly SentryGuardApiFactory _factory;

    public IaApiTests(SentryGuardApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> ClienteAsync(string email)
    {
        var client = _factory.WithWebHostBuilder(b => b.UseSetting("IA:ApiKey", "")).CreateClient();
        client.UseBearerToken(await client.LoginAsync(email));
        return client;
    }

    private static readonly object Ransomware = new
    {
        titulo = "Detección de artefacto LockBit y conexión a C2",
        descripcion = "Se detectó ejecutable anómalo intentando cifrar volumen y balizaje a servidor de comando y control."
    };

    [Fact]
    public async Task Clasificar_ConElServicioSimulado_DevuelveSeveridadYUnaCategoriaDelCatalogo()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");

        var respuesta = await admin.PostAsJsonAsync("/api/v1/ia/clasificar-incidente", Ransomware);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, cuerpo.GetProperty("severidadSugeridaId").GetInt32());
        Assert.Equal("Malware", cuerpo.GetProperty("categoriaSugerida").GetString());
        Assert.NotEmpty(cuerpo.GetProperty("accionesRecomendadas").EnumerateArray());

        // La categoría sugerida existe en tb_CategoriaIncidente: la interfaz puede completarla sola.
        var catalogo = await admin.GetFromJsonAsync<JsonElement>("/api/v1/catalogos/categorias-incidente");
        Assert.Contains(catalogo.GetProperty("items").EnumerateArray(), c => c.GetProperty("nombre").GetString() == "Malware");
    }

    [Theory]
    [InlineData("abc", "Descripcion suficientemente larga", "titulo")]
    [InlineData("Titulo valido de prueba", "corta", "descripcion")]
    public async Task Clasificar_ConEntradaInvalida_Devuelve400ConElCampo(string titulo, string descripcion, string campo)
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");

        var respuesta = await admin.PostAsJsonAsync("/api/v1/ia/clasificar-incidente", new { titulo, descripcion });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errores.TryGetProperty(campo, out _));
    }

    [Fact]
    public async Task Clasificar_RechazaTextosMasLargosQueElMaximo()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");

        var respuesta = await admin.PostAsJsonAsync("/api/v1/ia/clasificar-incidente", new
        {
            titulo = "Titulo valido de prueba",
            descripcion = new string('a', 5001)
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Ia_ExigeSesionYRespetaLosRoles()
    {
        var anonimo = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync("/api/v1/ia/clasificar-incidente", Ransomware)).StatusCode);

        var analista = await ClienteAsync("analista@sentryguard.cl");
        Assert.Equal(HttpStatusCode.Forbidden, (await analista.PostAsJsonAsync("/api/v1/ia/clasificar-incidente", Ransomware)).StatusCode);

        var cliente = await ClienteAsync("cliente@sentryguard.cl");
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/v1/ia/clasificar-incidente", Ransomware)).StatusCode);
        // El resumen del dashboard es solo para el staff que ve las métricas
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/v1/ia/resumir", new { contexto = "Resumen de 520 incidentes del ultimo anio" })).StatusCode);
    }

    [Fact]
    public async Task Resumir_ConElServicioSimulado_Devuelve200_YValidaElContexto()
    {
        var supervisor = await ClienteAsync("supervisor@sentryguard.cl");

        var ok = await supervisor.PostAsJsonAsync("/api/v1/ia/resumir", new { contexto = "Total de incidentes: 520. Cumplimiento SLA: 85%.", tipo = "dashboard" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resumen").GetString()));

        var invalido = await supervisor.PostAsJsonAsync("/api/v1/ia/resumir", new { contexto = "corto" });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Theory]
    [InlineData(504, "IA_TIEMPO_AGOTADO")]
    [InlineData(502, "IA_ERROR_PROVEEDOR")]
    public async Task ErroresDelProveedor_SeDevuelvenConElFormatoEstandar(int estado, string codigo)
    {
        var fabrica = _factory.WithWebHostBuilder(b => b.ConfigureServices(servicios =>
        {
            servicios.AddScoped<IAsistenteIA>(_ => new AsistenteQueFalla(estado));
        }));
        var client = fabrica.CreateClient();
        client.UseBearerToken(await client.LoginAsync("admin@sentryguard.cl"));

        var respuesta = await client.PostAsJsonAsync("/api/v1/ia/clasificar-incidente", Ransomware);

        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(estado, cuerpo.GetProperty("status").GetInt32());
        Assert.Equal(codigo, cuerpo.GetProperty("codigo").GetString());
        Assert.True(cuerpo.TryGetProperty("detail", out _));
    }

    // ---- Adaptador real (AsistenteGemini) contra un servidor HTTP simulado ----

    private sealed class AsistenteQueFalla(int estado) : IAsistenteIA
    {
        public Task<ServiceResult<ClasificacionIaDTO>> ClasificarIncidenteAsync(string titulo, string descripcion, CancellationToken cancellationToken = default) =>
            Task.FromResult(estado == 504 ? ServiceResult<ClasificacionIaDTO>.GatewayTimeout() : ServiceResult<ClasificacionIaDTO>.BadGateway());

        public Task<ServiceResult<ResumenIaDTO>> ResumirAsync(string contexto, string? tipo = "general", CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult<ResumenIaDTO>.BadGateway());
    }

    private sealed class ManejadorSimulado(Func<HttpRequestMessage, HttpResponseMessage> respuesta) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Peticiones { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Peticiones.Add(request);
            return Task.FromResult(respuesta(request));
        }
    }

    private static AsistenteGemini CrearAdaptador(ManejadorSimulado manejador, string? clave = "clave-de-prueba") =>
        new(new HttpClient(manejador),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["IA:ApiKey"] = clave }).Build(),
            NullLogger<AsistenteGemini>.Instance);

    private const string RespuestaDeGemini = """
        {"candidates":[{"content":{"parts":[{"text":"{\"severidadSugeridaId\":3,\"severidadNombre\":\"Alta\",\"categoriaSugerida\":\"Acceso no autorizado\",\"justificacion\":\"Explotacion web.\",\"accionesRecomendadas\":[\"Bloquear IP\"]}"}]}}]}
        """;

    [Fact]
    public async Task Gemini_EnviaLaClaveEnUnEncabezado_NuncaEnLaUrl()
    {
        var manejador = new ManejadorSimulado(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(RespuestaDeGemini) });

        var resultado = await CrearAdaptador(manejador).ClasificarIncidenteAsync("Inyeccion SQL en login", "Payloads detectados en el WAF del portal.");

        Assert.True(resultado.Success);
        Assert.Equal("Alta", resultado.Data!.SeveridadNombre);
        var peticion = Assert.Single(manejador.Peticiones);
        Assert.DoesNotContain("clave-de-prueba", peticion.RequestUri!.ToString());
        Assert.Equal("clave-de-prueba", Assert.Single(peticion.Headers.GetValues("x-goog-api-key")));
    }

    [Fact]
    public async Task Gemini_ConErrorDelProveedor_Devuelve502ConCodigo()
    {
        var manejador = new ManejadorSimulado(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var resultado = await CrearAdaptador(manejador).ClasificarIncidenteAsync("Phishing a ejecutivos", "Correos sospechosos pidiendo credenciales.");

        Assert.False(resultado.Success);
        Assert.Equal(502, resultado.StatusCode);
        Assert.Equal("IA_ERROR_PROVEEDOR", resultado.Codigo);
    }

    [Fact]
    public async Task Gemini_ConTiempoAgotado_Devuelve504()
    {
        var manejador = new ManejadorSimulado(_ => throw new TaskCanceledException("tiempo agotado"));

        var resultado = await CrearAdaptador(manejador).ClasificarIncidenteAsync("Phishing a ejecutivos", "Correos sospechosos pidiendo credenciales.");

        Assert.False(resultado.Success);
        Assert.Equal(504, resultado.StatusCode);
        Assert.Equal("IA_TIEMPO_AGOTADO", resultado.Codigo);
    }

    [Fact]
    public async Task Gemini_SinClave_NoLlamaAlProveedor()
    {
        var manejador = new ManejadorSimulado(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var resultado = await CrearAdaptador(manejador, clave: "").ClasificarIncidenteAsync("Phishing a ejecutivos", "Correos sospechosos pidiendo credenciales.");

        Assert.True(resultado.Success);
        Assert.Empty(manejador.Peticiones);
    }
}
