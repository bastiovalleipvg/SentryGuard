using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentryGuard.Data;
using SentryGuard.Models;

namespace SentryGuard.Tests;

/// <summary>
/// Hito 2: categoría de incidente, flujo de estados como datos (tb_TransicionEstado), auditoría por
/// entidad (RF-16), restricciones de integridad en la base y vistas SQL para reportes.
/// </summary>
public class AuditoriaCategoriaVistasApiTests : IClassFixture<SentryGuardApiFactory>
{
    private readonly SentryGuardApiFactory _factory;

    public AuditoriaCategoriaVistasApiTests(SentryGuardApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateClient();
        client.UseBearerToken(await client.LoginAsync("admin@sentryguard.cl"));
        return client;
    }

    private static async Task<int> CrearIncidenteAsync(HttpClient client, int? categoriaId = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = "Incidente de prueba de auditoria",
            descripcion = "Descripcion suficientemente larga",
            activoId = 1,
            severidadId = 2,
            categoriaId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<T> ConDbAsync<T>(Func<SentryGuardDbContext, Task<T>> accion)
    {
        using var scope = _factory.Services.CreateScope();
        return await accion(scope.ServiceProvider.GetRequiredService<SentryGuardDbContext>());
    }

    [Fact]
    public async Task Transiciones_VienenDeLaBaseDeDatos_YElCatalogoDeCategoriasEstaSembrado()
    {
        var admin = await AdminAsync();

        var transiciones = await admin.GetFromJsonAsync<JsonElement>("/api/v1/catalogos/transiciones-estado");
        var pares = transiciones.EnumerateArray()
            .Select(t => (t.GetProperty("estadoOrigenId").GetInt32(), t.GetProperty("estadoDestinoId").GetInt32()))
            .ToList();
        Assert.Equal(14, pares.Count);
        Assert.Contains((1, 2), pares);
        Assert.DoesNotContain((4, 3), pares);   // Resuelto no pasa a Escalado
        Assert.DoesNotContain((2, 1), pares);   // En progreso no vuelve a Abierto

        var categorias = await admin.GetFromJsonAsync<JsonElement>("/api/v1/catalogos/categorias-incidente");
        var nombres = categorias.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("nombre").GetString()).ToList();
        Assert.Equal(6, nombres.Count);
        Assert.Contains("Phishing", nombres);
        Assert.Contains("Fuga de datos", nombres);
    }

    [Fact]
    public async Task CambiarEstado_ConUnParNoPermitido_Devuelve409()
    {
        var admin = await AdminAsync();
        var id = await CrearIncidenteAsync(admin);

        (await admin.PostAsJsonAsync($"/api/v1/incidentes/{id}/cambiar-estado", new { estadoId = 4 })).EnsureSuccessStatusCode();
        var invalida = await admin.PostAsJsonAsync($"/api/v1/incidentes/{id}/cambiar-estado", new { estadoId = 3 });

        Assert.Equal(HttpStatusCode.Conflict, invalida.StatusCode);
        var cuerpo = await invalida.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TRANSICION_INVALIDA", cuerpo.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Incidente_GuardaCategoria_YRechazaUnaInexistente()
    {
        var admin = await AdminAsync();

        var id = await CrearIncidenteAsync(admin, categoriaId: 2);
        var incidente = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/incidentes/{id}");
        Assert.Equal("Malware", incidente.GetProperty("categoria").GetString());

        var invalida = await admin.PostAsJsonAsync("/api/v1/incidentes", new
        {
            titulo = "Incidente con categoria invalida",
            descripcion = "Descripcion suficientemente larga",
            activoId = 1,
            severidadId = 2,
            categoriaId = 999
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        var errores = (await invalida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errores.TryGetProperty("categoriaId", out _));

        var filtrados = await admin.GetFromJsonAsync<JsonElement>("/api/v1/incidentes?categoriaId=2&pageSize=50");
        Assert.All(filtrados.GetProperty("items").EnumerateArray(), i => Assert.Equal("Malware", i.GetProperty("categoria").GetString()));
    }

    [Fact]
    public async Task Auditoria_RegistraQuienYCuandoCreoYModificoElIncidente_ConValoresAnterioresYNuevos()
    {
        var admin = await AdminAsync();
        var id = await CrearIncidenteAsync(admin);

        (await admin.PutAsJsonAsync($"/api/v1/incidentes/{id}", new { severidadId = 4 })).EnsureSuccessStatusCode();

        var incidente = await ConDbAsync(db => db.Incidentes.AsNoTracking().FirstAsync(i => i.Id == id));
        Assert.Equal(1, incidente.CreadoPorId);
        Assert.Equal(1, incidente.ModificadoPorId);
        Assert.True(incidente.FechaModificacion >= incidente.FechaCreacion);

        var logs = await ConDbAsync(db => db.LogsAuditoria.AsNoTracking()
            .Where(l => l.Entidad == "Incidente" && l.EntidadId == id).OrderBy(l => l.Id).ToListAsync());
        Assert.Contains(logs, l => l.Accion == "Incidente creado" && l.UsuarioId == 1);
        var modificado = Assert.Single(logs, l => l.Accion == "Incidente modificado");
        Assert.Contains("\"SeveridadId\":2", modificado.ValoresAnteriores);
        Assert.Contains("\"SeveridadId\":4", modificado.ValoresNuevos);

        // El administrador puede consultar el detalle por entidad desde la API
        var api = await admin.GetFromJsonAsync<JsonElement>("/api/v1/auditoria?entidad=Incidente&pageSize=50");
        Assert.Contains(api.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("entidadId").GetInt32() == id && e.GetProperty("accion").GetString() == "Incidente modificado");
    }

    [Fact]
    public async Task BaseDeDatos_RechazaIncidenteDeOtraOrganizacionYCierreAnteriorALaCreacion()
    {
        var admin = await AdminAsync();
        var id = await CrearIncidenteAsync(admin);

        // El activo 1 es de la organización 1: un incidente que declare otra organización viola la FK compuesta.
        var otraOrg = await ConDbAsync(async db =>
        {
            var org = new Organizacion { RazonSocial = "Otra Organizacion SpA", Rut = $"9{Random.Shared.Next(1_000_000, 9_999_999)}-K", PlanContratadoId = 1 };
            db.Organizaciones.Add(org);
            await db.SaveChangesAsync();
            return org.Id;
        });

        var excepcionOrg = await Assert.ThrowsAsync<DbUpdateException>(() => ConDbAsync(async db =>
        {
            db.Incidentes.Add(new Incidente
            {
                OrganizacionId = otraOrg, ActivoId = 1, Titulo = "Cruce de organizaciones", Descripcion = "Descripcion larga de prueba",
                SeveridadId = 1, EstadoId = 1
            });
            return await db.SaveChangesAsync();
        }));
        Assert.Contains("FK_tb_Incidente_Activo_Organizacion", excepcionOrg.InnerException?.Message);

        var excepcionCierre = await Assert.ThrowsAsync<DbUpdateException>(() => ConDbAsync(async db =>
        {
            var incidente = await db.Incidentes.FirstAsync(i => i.Id == id);
            incidente.FechaCierre = incidente.FechaCreacion.AddHours(-1);
            return await db.SaveChangesAsync();
        }));
        Assert.Contains("CK_tb_Incidente_FechaCierre", excepcionCierre.InnerException?.Message);
    }

    [Fact]
    public async Task Vistas_ConsolidanElDetalleElSlaYLosIndicadoresMensuales()
    {
        var admin = await AdminAsync();
        var id = await CrearIncidenteAsync(admin, categoriaId: 1);

        var (detalle, sla, mensual, totalIncidentes) = await ConDbAsync(async db => (
            await db.VwIncidenteDetalle.AsNoTracking().FirstAsync(v => v.Id == id),
            await db.VwCumplimientoSla.AsNoTracking().FirstAsync(v => v.IncidenteId == id),
            await db.VwIndicadoresMensuales.AsNoTracking().ToListAsync(),
            await db.Incidentes.CountAsync()));

        Assert.Equal("Empresa Demo SpA", detalle.Organizacion);
        Assert.Equal("Phishing", detalle.CategoriaIncidente);
        Assert.Equal("Media", detalle.Severidad);
        Assert.Null(detalle.HorasResolucion); // sigue abierto

        Assert.Equal(12m, sla.HorasSla);       // severidad Media = 12 h (sin contrato vigente)
        Assert.True(sla.CumpleSla);

        Assert.Equal(totalIncidentes, mensual.Sum(m => m.TotalIncidentes));
    }
}
