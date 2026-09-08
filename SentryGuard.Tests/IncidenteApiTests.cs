using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SentryGuard.Tests;

/// <summary>
/// Tests de integracion para los endpoints principales de Incidente: codigos de estado
/// correctos, aislamiento entre organizaciones (OWASP API1) y RBAC (endpoints protegidos
/// rechazan requests sin token o con rol insuficiente).
/// </summary>
public class IncidenteApiTests : IClassFixture<SentryGuardApiFactory>
{
    private readonly SentryGuardApiFactory _factory;

    public IncidenteApiTests(SentryGuardApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> GetIdAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task ListarIncidentes_SinToken_Devuelve401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/incidentes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ObtenerIncidente_SinToken_Devuelve401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/incidentes/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EliminarIncidente_ConRolInsuficiente_Devuelve403()
    {
        var client = _factory.CreateClient();
        var supervisorToken = await client.LoginAsync("supervisor@sentryguard.cl");
        client.UseBearerToken(supervisorToken);

        // El rol se valida antes de tocar la base de datos, asi que el id no necesita existir.
        var response = await client.DeleteAsync("/api/v1/incidentes/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CrearIncidente_ComoCliente_DevuelveCreated()
    {
        var adminClient = _factory.CreateClient();
        adminClient.UseBearerToken(await adminClient.LoginAsync("admin@sentryguard.cl"));

        var activoResp = await adminClient.PostAsJsonAsync("/api/v1/activos", new
        {
            organizacionId = 1,
            nombre = "Activo Test - Crear Incidente",
            hostnameIp = "10.0.0.11",
            categoriaId = 1
        });
        activoResp.EnsureSuccessStatusCode();
        var activoId = await GetIdAsync(activoResp);

        var clienteClient = _factory.CreateClient();
        clienteClient.UseBearerToken(await clienteClient.LoginAsync("cliente@sentryguard.cl"));

        var response = await clienteClient.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = "Incidente de prueba",
            descripcion = "Descripcion de prueba",
            activoId,
            severidadId = 2
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/v1/incidentes/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ObtenerIncidente_Inexistente_Devuelve404()
    {
        var adminClient = _factory.CreateClient();
        adminClient.UseBearerToken(await adminClient.LoginAsync("admin@sentryguard.cl"));

        var response = await adminClient.GetAsync("/api/v1/incidentes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cliente_NoPuedeVerIncidenteDeOtraOrganizacion()
    {
        var adminClient = _factory.CreateClient();
        adminClient.UseBearerToken(await adminClient.LoginAsync("admin@sentryguard.cl"));

        // Organizacion, activo, cliente e incidente nuevos, aislados de otras organizaciones.
        var orgResp = await adminClient.PostAsJsonAsync("/api/v1/organizaciones", new
        {
            razonSocial = "Otra Empresa Test",
            rut = "88888888-8",
            planId = 1
        });
        orgResp.EnsureSuccessStatusCode();
        var orgId = await GetIdAsync(orgResp);

        var activoResp = await adminClient.PostAsJsonAsync("/api/v1/activos", new
        {
            organizacionId = orgId,
            nombre = "Activo Otra Organizacion",
            hostnameIp = "10.0.0.20",
            categoriaId = 1
        });
        activoResp.EnsureSuccessStatusCode();
        var activoId = await GetIdAsync(activoResp);

        var email = $"cliente.otraorg.{Guid.NewGuid():N}@sentryguard.cl";
        var registroResp = await adminClient.PostAsJsonAsync("/api/v1/usuarios/registro", new
        {
            nombre = "Cliente de otra organizacion",
            email,
            password = "OtraOrg123!",
            rolId = 4,
            organizacionId = orgId
        });
        registroResp.EnsureSuccessStatusCode();

        var clienteOtraOrgClient = _factory.CreateClient();
        clienteOtraOrgClient.UseBearerToken(await clienteOtraOrgClient.LoginAsync(email, "OtraOrg123!"));

        var incResp = await clienteOtraOrgClient.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = "Incidente de otra organizacion",
            descripcion = "No deberia ser visible para el cliente de la organizacion 1",
            activoId,
            severidadId = 2
        });
        incResp.EnsureSuccessStatusCode();
        var incidenteId = await GetIdAsync(incResp);

        // El Cliente de la organizacion demo (id=1, seed del punto 1) intenta ver ese incidente.
        var clienteOrg1Client = _factory.CreateClient();
        clienteOrg1Client.UseBearerToken(await clienteOrg1Client.LoginAsync("cliente@sentryguard.cl"));

        var response = await clienteOrg1Client.GetAsync($"/api/v1/incidentes/{incidenteId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
