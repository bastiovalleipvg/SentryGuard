using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentryGuard.Data;
using System.Security.Cryptography;

namespace SentryGuard.Tests;

/// <summary>
/// WebApplicationFactory que levanta la API completa (JWT, RBAC, rate limiting, etc.) contra
/// una base de datos SQL Server dedicada a tests (SentryGuardDB_Test, ver appsettings.Testing.json),
/// recreada desde las migraciones (incluyendo el seeding de catalogos/usuarios de prueba) antes
/// de correr cada clase de tests.
/// </summary>
public class SentryGuardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Mitigacion de credencial fija en tests: genera una clave JWT aleatoria y segura para cada suite
        var jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        builder.UseSetting("Jwt:Key", jwtKey);
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SentryGuardDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
