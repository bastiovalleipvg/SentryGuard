using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

// Todas las clases de test recrean la misma BD SentryGuardDB_Test: deben correr en serie.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SentryGuard.Tests;

/// <summary>
/// Controles agregados para el consumo desde navegador (Trabajo 2): validacion de entradas en el
/// servidor, errores sin detalles internos, login sin enumeracion de cuentas, CORS restringido,
/// cabeceras de seguridad y paginacion acotada.
/// </summary>
public class SeguridadApiTests : IClassFixture<SentryGuardApiFactory>
{
    private const string OrigenPermitido = "https://localhost:5173";
    private readonly SentryGuardApiFactory _factory;

    public SeguridadApiTests(SentryGuardApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> ClienteAutenticadoAsync(string email)
    {
        var client = _factory.CreateClient();
        client.UseBearerToken(await client.LoginAsync(email));
        return client;
    }

    [Fact]
    public async Task CrearIncidente_ConTituloVacio_Devuelve400ConErrorDeCampo()
    {
        var client = await ClienteAutenticadoAsync("admin@sentryguard.cl");

        var response = await client.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = "",
            descripcion = "corta",
            activoId = 999999,
            severidadId = 999999
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errores = json.GetProperty("errors");
        Assert.True(errores.TryGetProperty("titulo", out _));
        Assert.True(errores.TryGetProperty("descripcion", out _));
        Assert.True(errores.TryGetProperty("activoId", out _));
        Assert.True(errores.TryGetProperty("severidadId", out _));
    }

    [Fact]
    public async Task CrearIncidente_ConJsonMalFormado_NoFiltraDetallesInternos()
    {
        var client = await ClienteAutenticadoAsync("admin@sentryguard.cl");

        var contenido = new StringContent("{ \"titulo\": ", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/incidentes", contenido);
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("Exception", cuerpo);
        Assert.DoesNotContain("System.", cuerpo);
        Assert.DoesNotContain("   at ", cuerpo);
    }

    [Fact]
    public async Task Login_EmailInexistenteYPasswordIncorrecta_DevuelvenMismoMensaje()
    {
        var client = _factory.CreateClient();

        var inexistente = await client.PostAsJsonAsync("/api/v1/usuarios/login",
            new { email = "nadie@sentryguard.cl", password = "Cualquiera123!" });
        var incorrecta = await client.PostAsJsonAsync("/api/v1/usuarios/login",
            new { email = "supervisor@sentryguard.cl", password = "Incorrecta123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, incorrecta.StatusCode);
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await incorrecta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cors_OrigenPermitido_RecibeAccessControlAllowOrigin()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/usuarios/login");
        request.Headers.Add("Origin", OrigenPermitido);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores));
        Assert.Equal(OrigenPermitido, valores!.Single());
    }

    [Fact]
    public async Task Cors_OrigenNoPermitido_NoRecibeAccessControlAllowOrigin()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/usuarios/login");
        request.Headers.Add("Origin", "https://sitio-malicioso.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task RespuestasDeApi_IncluyenCabecerasDeSeguridad()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/catalogos/severidades");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task ListarIncidentes_PageSizeExcesivo_SeAcotaA50()
    {
        var client = await ClienteAutenticadoAsync("admin@sentryguard.cl");

        var json = await client.GetFromJsonAsync<JsonElement>("/api/v1/incidentes?pageSize=100000");

        Assert.Equal(50, json.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task ListarIncidentes_ConBusqueda_FiltraPorTitulo()
    {
        var admin = await ClienteAutenticadoAsync("admin@sentryguard.cl");
        var activoResp = await admin.PostAsJsonAsync("/api/v1/activos", new
        {
            organizacionId = 1,
            nombre = "Activo busqueda",
            hostnameIp = "10.0.0.30",
            categoriaId = 1
        });
        activoResp.EnsureSuccessStatusCode();
        var activoId = (await activoResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var marcador = $"Ransomware-{Guid.NewGuid():N}"[..20];
        (await admin.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = $"{marcador} en servidor",
            descripcion = "Descripcion suficientemente larga",
            activoId,
            severidadId = 3
        })).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = "Phishing a contabilidad",
            descripcion = "Descripcion suficientemente larga",
            activoId,
            severidadId = 2
        })).EnsureSuccessStatusCode();

        var json = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/incidentes?q={marcador}");

        Assert.Equal(1, json.GetProperty("total").GetInt32());
        Assert.StartsWith(marcador, json.GetProperty("items")[0].GetProperty("titulo").GetString());
    }

    [Fact]
    public async Task Login_Exitoso_GuardaRefreshTokenHasheadoEnBD()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/usuarios/login", new
        {
            email = "admin@sentryguard.cl",
            password = "Admin123!"
        });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.TryGetProperty("token", out var tokenProp));
        Assert.True(json.TryGetProperty("refreshToken", out var refreshProp));
        var rawRefreshToken = refreshProp.GetString();
        Assert.NotNull(rawRefreshToken);

        // Verifica en la base de datos que el token_hash NO sea el texto plano, sino su hash SHA-256
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SentryGuard.Data.SentryGuardDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<SentryGuard.Services.AuthService>();

        var expectedHash = authService.HashearRefreshToken(rawRefreshToken);
        var guardado = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.Set<SentryGuard.Models.RefreshToken>(),
            rt => rt.TokenHash == expectedHash);

        Assert.NotNull(guardado);
        Assert.NotEqual(rawRefreshToken, guardado.TokenHash);
        Assert.Equal(expectedHash, guardado.TokenHash);
    }

    [Fact]
    public async Task UsuarioDesactivado_PierdeElAccesoDeInmediato_AunConTokenVigente()
    {
        // 1. Iniciar sesion con analista y obtener token vigente
        var client = _factory.CreateClient();
        var token = await client.LoginAsync("analista@sentryguard.cl");
        client.UseBearerToken(token);

        // 2. Verificar que inicialmente tiene acceso
        var meInicial = await client.GetAsync("/api/v1/usuarios/me");
        Assert.Equal(HttpStatusCode.OK, meInicial.StatusCode);

        // 3. Desactivar el usuario en la base de datos
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SentryGuard.Data.SentryGuardDbContext>();
            var usuario = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(
                db.Usuarios, u => u.Email == "analista@sentryguard.cl");
            usuario.Activo = false;
            await db.SaveChangesAsync();
        }

        // 4. Intentar acceder con el mismo token (vigente): debe ser rechazado inmediatamente
        var meDesactivado = await client.GetAsync("/api/v1/usuarios/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meDesactivado.StatusCode);

        // 5. Restaurar estado para no alterar otros tests
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SentryGuard.Data.SentryGuardDbContext>();
            var usuario = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(
                db.Usuarios, u => u.Email == "analista@sentryguard.cl");
            usuario.Activo = true;
            await db.SaveChangesAsync();
        }
    }
}
