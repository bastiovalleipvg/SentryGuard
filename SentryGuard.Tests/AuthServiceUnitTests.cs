using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SentryGuard.Models;
using SentryGuard.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SentryGuard.Tests
{
    /// <summary>
    /// Pruebas unitarias para AuthService (OWASP API1, API2 y control de acceso RBAC).
    /// </summary>
    public class AuthServiceUnitTests
    {
        private readonly IConfiguration _config;
        private readonly AuthService _authService;

        public AuthServiceUnitTests()
        {
            var configValues = new Dictionary<string, string?>
            {
                { "Jwt:Key", "ClaveSecretaSuperSeguraParaPruebasUnitariasDeAlMenos64BytesDeLargoTotal!" },
                { "Jwt:Issuer", "https://api.sentryguard.cl" },
                { "Jwt:Audience", "https://sentryguard.cl" },
                { "Jwt:ExpireMinutes", "15" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            _authService = new AuthService(_config);
        }

        private Data.SentryGuardDbContext CrearDbContextInMemory()
        {
            var options = new DbContextOptionsBuilder<Data.SentryGuardDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new Data.SentryGuardDbContext(options);
        }

        [Fact]
        public void HashPassword_GeneraHashNoVacioYVerificable()
        {
            // Arrange
            var usuario = new Usuario { Id = 1, Email = "test@sentryguard.cl" };
            var passwordPlano = "PasswordSeguro123!";

            // Act
            var hash = _authService.HashPassword(usuario, passwordPlano);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.NotEqual(passwordPlano, hash);
        }

        [Fact]
        public async Task VerifyPassword_ContrasenaCorrecta_DevuelveValidaYReseteaIntentos()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "Usuario Test",
                Email = "test@sentryguard.cl",
                IntentosFallidos = 3
            };
            usuario.PasswordHash = _authService.HashPassword(usuario, "Admin123!");
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            // Act
            var (esValida, razon) = await _authService.VerifyPasswordAsync(usuario, "Admin123!", db);

            // Assert
            Assert.True(esValida);
            Assert.Null(razon);
            Assert.Equal(0, usuario.IntentosFallidos);
            Assert.Null(usuario.BloqueadoHasta);
        }

        [Fact]
        public async Task VerifyPassword_ContrasenaIncorrecta_IncrementaIntentosFallidos()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "Usuario Test",
                Email = "test@sentryguard.cl",
                IntentosFallidos = 0
            };
            usuario.PasswordHash = _authService.HashPassword(usuario, "Admin123!");
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            // Act
            var (esValida, razon) = await _authService.VerifyPasswordAsync(usuario, "ContrasenaEquivocada", db);

            // Assert
            Assert.False(esValida);
            Assert.NotNull(razon);
            Assert.Equal(1, usuario.IntentosFallidos);
        }

        [Fact]
        public async Task VerifyPassword_CincoIntentosFallidos_BloqueaUsuarioTemporalmente()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "Usuario Test",
                Email = "test@sentryguard.cl",
                IntentosFallidos = 4
            };
            usuario.PasswordHash = _authService.HashPassword(usuario, "Admin123!");
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            // Act: 5to intento fallido
            var (esValida, razon) = await _authService.VerifyPasswordAsync(usuario, "Erronea", db);

            // Assert
            Assert.False(esValida);
            Assert.Equal(5, usuario.IntentosFallidos);
            Assert.NotNull(usuario.BloqueadoHasta);
            Assert.True(usuario.BloqueadoHasta > DateTime.UtcNow);
            Assert.Contains("bloqueado", razon);
        }

        [Fact]
        public async Task VerifyPassword_UsuarioBloqueado_RechazaIntentoInmediatamente()
        {
            // Arrange
            using var db = CrearDbContextInMemory();
            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "Usuario Test",
                Email = "test@sentryguard.cl",
                BloqueadoHasta = DateTime.UtcNow.AddMinutes(10)
            };
            usuario.PasswordHash = _authService.HashPassword(usuario, "Admin123!");
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            // Act: intento con contraseña correcta mientras está bloqueado
            var (esValida, razon) = await _authService.VerifyPasswordAsync(usuario, "Admin123!", db);

            // Assert
            Assert.False(esValida);
            Assert.Contains("bloqueado", razon);
        }

        [Fact]
        public void GenerateJwtToken_IncluyeClaimsDeRolOrganizacionYUsuario()
        {
            // Arrange
            var rol = new Rol { Id = 4, Nombre = "Cliente" };
            var usuario = new Usuario
            {
                Id = 42,
                Nombre = "Cliente Test",
                Email = "cliente@test.cl",
                OrganizacionId = 7
            };

            // Act
            var tokenString = _authService.GenerateJwtToken(usuario, rol);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(tokenString));

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenString);

            Assert.Equal("https://api.sentryguard.cl", jwt.Issuer);
            Assert.Contains("https://sentryguard.cl", jwt.Audiences);

            var claims = jwt.Claims;
            Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
            Assert.Contains(claims, c => c.Type == ClaimTypes.Email && c.Value == "cliente@test.cl");
            Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Cliente");
            Assert.Contains(claims, c => c.Type == "organizacion_id" && c.Value == "7");
        }

        [Fact]
        public void HashearRefreshToken_CalculaSha256Determinista()
        {
            // Arrange
            var token = "token-de-prueba-refresh-12345";

            // Act
            var hash1 = _authService.HashearRefreshToken(token);
            var hash2 = _authService.HashearRefreshToken(token);

            // Assert
            Assert.Equal(64, hash1.Length); // 256 bits = 64 hex chars
            Assert.Equal(hash1, hash2);
            Assert.NotEqual(token, hash1);
        }

        [Fact]
        public void ValidaAccesoOrganizacion_AdministradorYSupervisor_AccesoTotal()
        {
            // Arrange
            var admin = new Usuario { Rol = new Rol { Nombre = "Administrador" } };
            var supervisor = new Usuario { Rol = new Rol { Nombre = "Supervisor" } };

            // Act & Assert
            Assert.True(_authService.ValidaAccesoOrganizacion(admin, 999));
            Assert.True(_authService.ValidaAccesoOrganizacion(supervisor, 999));
        }

        [Fact]
        public void ValidaAccesoOrganizacion_Cliente_SoloSuOrganizacion()
        {
            // Arrange
            var cliente = new Usuario { OrganizacionId = 5, Rol = new Rol { Nombre = "Cliente" } };

            // Act & Assert
            Assert.True(_authService.ValidaAccesoOrganizacion(cliente, 5));
            Assert.False(_authService.ValidaAccesoOrganizacion(cliente, 8));
        }

        [Fact]
        public void ValidaAccesoIncidente_Analista_SoloAsignados()
        {
            // Arrange
            var analista = new Usuario { Id = 3, Rol = new Rol { Nombre = "Analista" } };
            var incidenteAsignado = new Incidente { Id = 1, AnalistaAsignadoId = 3 };
            var incidenteAjeno = new Incidente { Id = 2, AnalistaAsignadoId = 99 };

            // Act & Assert
            Assert.True(_authService.ValidaAccesoIncidente(analista, incidenteAsignado));
            Assert.False(_authService.ValidaAccesoIncidente(analista, incidenteAjeno));
        }

        [Fact]
        public void ValidaAccesoIncidente_Cliente_SoloMismaOrganizacion()
        {
            // Arrange
            var cliente = new Usuario { OrganizacionId = 2, Rol = new Rol { Nombre = "Cliente" } };
            var incidenteMismaOrg = new Incidente { Id = 1, OrganizacionId = 2 };
            var incidenteOtraOrg = new Incidente { Id = 2, OrganizacionId = 9 };

            // Act & Assert
            Assert.True(_authService.ValidaAccesoIncidente(cliente, incidenteMismaOrg));
            Assert.False(_authService.ValidaAccesoIncidente(cliente, incidenteOtraOrg));
        }
    }
}
