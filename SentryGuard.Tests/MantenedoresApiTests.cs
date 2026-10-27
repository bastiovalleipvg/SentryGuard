using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SentryGuard.Tests;

/// <summary>
/// Recursos relacionados que consume el frontend del Trabajo 2: activos, organizaciones y contratos SLA,
/// usuarios, catalogos, bitacora, evidencias, cumplimiento SLA y auditoria. Cubre validacion (400),
/// RBAC (403), aislamiento por organizacion (OWASP API1) y borrados con datos asociados (409).
/// </summary>
public class MantenedoresApiTests : IClassFixture<SentryGuardApiFactory>
{
    private readonly SentryGuardApiFactory _factory;

    public MantenedoresApiTests(SentryGuardApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> ClienteAsync(string email, string password = "Admin123!")
    {
        var client = _factory.CreateClient();
        client.UseBearerToken(await client.LoginAsync(email, password));
        return client;
    }

    private static async Task<int> IdAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<JsonElement> ErroresAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
    }

    // RUT aleatorio con digito verificador valido (modulo 11), para no chocar con la regla de RUT unico.
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

    private static async Task<int> CrearActivoAsync(HttpClient admin, int organizacionId = 1) =>
        await IdAsync(await admin.PostAsJsonAsync("/api/v1/activos", new
        {
            organizacionId,
            nombre = $"Activo {Guid.NewGuid():N}"[..20],
            hostnameIp = "10.0.0.50",
            categoriaId = 1
        }));

    private static async Task<int> CrearIncidenteAsync(HttpClient client, int activoId, int severidadId = 3) =>
        await IdAsync(await client.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = "Incidente de prueba de mantenedores",
            descripcion = "Descripcion suficientemente larga",
            activoId,
            severidadId
        }));

    [Fact]
    public async Task EliminarActivo_ConIncidentes_Devuelve409_YSinIncidentes_204()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");
        var conIncidente = await CrearActivoAsync(admin);
        await CrearIncidenteAsync(admin, conIncidente);
        var libre = await CrearActivoAsync(admin);

        var conflicto = await admin.DeleteAsync($"/api/v1/activos/{conIncidente}");
        Assert.Equal(HttpStatusCode.Conflict, conflicto.StatusCode);
        Assert.Equal("ACTIVO_CON_INCIDENTES", (await conflicto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/activos/{libre}")).StatusCode);
    }

    [Fact]
    public async Task Activo_ValidaCampos_YSupervisorNoPuedeEliminar()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");
        var errores = await ErroresAsync(await admin.PostAsJsonAsync("/api/v1/activos", new
        {
            organizacionId = 999999,
            nombre = "x",
            hostnameIp = "<script>alert(1)</script>",
            categoriaId = 999999
        }));
        Assert.True(errores.TryGetProperty("nombre", out _));
        Assert.True(errores.TryGetProperty("hostnameIp", out _));
        Assert.True(errores.TryGetProperty("categoriaId", out _));
        Assert.True(errores.TryGetProperty("organizacionId", out _));

        var activoId = await CrearActivoAsync(admin);
        var supervisor = await ClienteAsync("supervisor@sentryguard.cl");
        Assert.Equal(HttpStatusCode.Forbidden, (await supervisor.DeleteAsync($"/api/v1/activos/{activoId}")).StatusCode);
        var cliente = await ClienteAsync("cliente@sentryguard.cl");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/v1/activos", new { organizacionId = 1, nombre = "Intento", categoriaId = 1 })).StatusCode);
    }

    [Fact]
    public async Task Organizacion_RechazaRutInvalido_YRutDuplicado()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");

        var dvMalo = await ErroresAsync(await admin.PostAsJsonAsync("/api/v1/organizaciones", new { razonSocial = "Empresa DV", rut = "76123456-7", planId = 1 }));
        Assert.True(dvMalo.TryGetProperty("rut", out _));

        var rut = RutValido();
        await IdAsync(await admin.PostAsJsonAsync("/api/v1/organizaciones", new { razonSocial = "Empresa Original", rut, planId = 1 }));
        var duplicado = await ErroresAsync(await admin.PostAsJsonAsync("/api/v1/organizaciones", new { razonSocial = "Empresa Copia", rut, planId = 2 }));
        Assert.Contains("Ya existe", duplicado.GetProperty("rut")[0].GetString());
    }

    [Fact]
    public async Task ContratoSla_SeAplicaAlCumplimiento_YClienteNoVeContratosAjenos()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");
        var orgId = await IdAsync(await admin.PostAsJsonAsync("/api/v1/organizaciones", new { razonSocial = "Empresa con contrato", rut = RutValido(), planId = 3 }));

        var contrato = await admin.PostAsJsonAsync($"/api/v1/organizaciones/{orgId}/contratos-sla", new
        {
            vigenteDesde = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
            detalles = new[] { new { severidadId = 3, horasSla = 2.5m } }
        });
        Assert.Equal(HttpStatusCode.Created, contrato.StatusCode);

        // Un segundo contrato abierto se superpone con el vigente.
        var superpuesto = await ErroresAsync(await admin.PostAsJsonAsync($"/api/v1/organizaciones/{orgId}/contratos-sla", new
        {
            vigenteDesde = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            detalles = new[] { new { severidadId = 1, horasSla = 10m } }
        }));
        Assert.True(superpuesto.TryGetProperty("vigenteDesde", out _));

        var incidenteId = await CrearIncidenteAsync(admin, await CrearActivoAsync(admin, orgId), severidadId: 3);
        var sla = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/incidentes/sla/cumplimiento/{incidenteId}");
        Assert.Equal("Contrato SLA", sla.GetProperty("fuente").GetString());
        Assert.Equal(2.5, sla.GetProperty("horasSLA").GetDouble());

        var cliente = await ClienteAsync("cliente@sentryguard.cl");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync($"/api/v1/organizaciones/{orgId}/contratos-sla")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/organizaciones/1/contratos-sla")).StatusCode);
    }

    [Fact]
    public async Task Usuarios_SoloAdministrador_YSinPasswordHash()
    {
        var supervisor = await ClienteAsync("supervisor@sentryguard.cl");
        Assert.Equal(HttpStatusCode.Forbidden, (await supervisor.GetAsync("/api/v1/usuarios")).StatusCode);

        var admin = await ClienteAsync("admin@sentryguard.cl");
        var respuesta = await admin.GetAsync("/api/v1/usuarios?pageSize=50");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("admin@sentryguard.cl", cuerpo);
        Assert.DoesNotContain("password", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registro_ValidaPassword_YOrganizacionDelCliente()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");
        var errores = await ErroresAsync(await admin.PostAsJsonAsync("/api/v1/usuarios/registro", new
        {
            nombre = "Cliente sin organizacion",
            email = "admin@sentryguard.cl",
            password = "debil",
            rolId = 4,
            organizacionId = (int?)null
        }));
        Assert.True(errores.TryGetProperty("email", out _));
        Assert.True(errores.TryGetProperty("password", out _));
        Assert.True(errores.TryGetProperty("organizacionId", out _));
    }

    [Fact]
    public async Task UsuarioDesactivado_NoPuedeIniciarSesion_YAdminNoSeDesactivaASiMismo()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");
        var email = $"analista.{Guid.NewGuid():N}@sentryguard.cl";
        var id = await IdAsync(await admin.PostAsJsonAsync("/api/v1/usuarios/registro", new { nombre = "Analista temporal", email, password = "Temporal123!", rolId = 3 }));

        var desactivar = await admin.PutAsJsonAsync($"/api/v1/usuarios/{id}", new { nombre = "Analista temporal", rolId = 3, organizacionId = (int?)null, activo = false });
        Assert.Equal(HttpStatusCode.OK, desactivar.StatusCode);

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/usuarios/login", new { email, password = "Temporal123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        var aSiMismo = await ErroresAsync(await admin.PutAsJsonAsync("/api/v1/usuarios/1", new { nombre = "Administrador SentryGuard", rolId = 1, organizacionId = (int?)null, activo = false }));
        Assert.True(aSiMismo.TryGetProperty("activo", out _));
    }

    [Fact]
    public async Task Bitacora_RegistraAccionesDelFlujo_YEvidenciaRechazaEsquemasPeligrosos()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");
        var incidenteId = await CrearIncidenteAsync(admin, await CrearActivoAsync(admin));

        // Sin evidencias: lista vacia (200), no 404.
        var sinEvidencias = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/incidentes/{incidenteId}/evidencia");
        Assert.Equal(0, sinEvidencias.GetProperty("total").GetInt32());

        (await admin.PostAsJsonAsync($"/api/v1/incidentes/{incidenteId}/cambiar-estado", new { estadoId = 2 })).EnsureSuccessStatusCode();
        var bitacora = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/incidentes/{incidenteId}/bitacora");
        var acciones = bitacora.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("accion").GetString()).ToList();
        Assert.Contains("Incidente creado", acciones);
        Assert.Contains("Cambio de estado", acciones);

        var peligrosa = await ErroresAsync(await admin.PostAsJsonAsync($"/api/v1/incidentes/{incidenteId}/evidencia", new
        {
            nombreArchivo = "captura.png",
            rutaAlmacenamiento = "javascript:alert(document.cookie)"
        }));
        Assert.True(peligrosa.TryGetProperty("rutaAlmacenamiento", out _));

        var valida = await admin.PostAsJsonAsync($"/api/v1/incidentes/{incidenteId}/evidencia", new
        {
            nombreArchivo = "captura.png",
            rutaAlmacenamiento = "https://evidencias.sentryguard.cl/inc/captura.png"
        });
        Assert.Equal(HttpStatusCode.Created, valida.StatusCode);
    }

    [Fact]
    public async Task Catalogos_EliminarEnUsoDevuelve409_YAuditoriaSoloAdministrador()
    {
        var admin = await ClienteAsync("admin@sentryguard.cl");
        // Categoria 1 (Servidor) la usan los activos de demostracion.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync("/api/v1/catalogos/categorias-activo/1")).StatusCode);

        var duplicada = await ErroresAsync(await admin.PostAsJsonAsync("/api/v1/catalogos/categorias-activo", new { nombre = "servidor" }));
        Assert.True(duplicada.TryGetProperty("nombre", out _));

        var analista = await ClienteAsync("analista@sentryguard.cl");
        Assert.Equal(HttpStatusCode.Forbidden, (await analista.GetAsync("/api/v1/auditoria")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/auditoria")).StatusCode);
    }
}
