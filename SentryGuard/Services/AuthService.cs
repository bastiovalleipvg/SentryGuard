using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SentryGuard.Data;
using SentryGuard.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SentryGuard.Services
{
    /// <summary>
    /// Servicio de autenticación con JWT, hash seguro y bloqueo de usuario.
    /// Mitigación OWASP API2: JWT corto + refresh + bcrypt + bloqueo tras intentos fallidos.
    /// </summary>
    public class AuthService
    {
        private readonly PasswordHasher<Usuario> _hasher = new();
        private readonly IConfiguration _configuration;
        private const int MAX_INTENTOS_FALLIDOS = 5;
        private const int MINUTOS_BLOQUEO = 15;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Hash seguro de contraseña (OWASP API2).
        /// </summary>
        public string HashPassword(Usuario usuario, string password)
        {
            return _hasher.HashPassword(usuario, password);
        }

        /// <summary>
        /// Verifica contraseña y maneja bloqueo tras intentos fallidos (OWASP API2).
        /// </summary>
        public async Task<(bool esValida, string? razon)> VerifyPasswordAsync(Usuario usuario, string passwordIntento, SentryGuardDbContext db)
        {
            if (usuario.BloqueadoHasta > DateTime.UtcNow)
                return (false, "Usuario bloqueado temporalmente. Intente más tarde.");

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, passwordIntento);

            if (resultado == PasswordVerificationResult.Failed)
            {
                // Incrementar intentos fallidos
                usuario.IntentosFallidos++;

                if (usuario.IntentosFallidos >= MAX_INTENTOS_FALLIDOS)
                {
                    usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(MINUTOS_BLOQUEO);
                    await db.SaveChangesAsync();
                    return (false, $"Demasiados intentos fallidos. Usuario bloqueado por {MINUTOS_BLOQUEO} minutos.");
                }

                await db.SaveChangesAsync();
                return (false, $"Contraseña incorrecta. Intento {usuario.IntentosFallidos}/{MAX_INTENTOS_FALLIDOS}");
            }

            // Contraseña correcta: resetear intentos fallidos
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            await db.SaveChangesAsync();

            return (true, null);
        }

        /// <summary>
        /// Genera JWT con claims para RBAC (OWASP API2, API5).
        /// Token con expiración corta.
        /// </summary>
        public string GenerateJwtToken(Usuario usuario, Rol rol)
        {
            var jwtKey = _configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key en appsettings.json");
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var jwtExpireMinutes = _configuration["Jwt:ExpireMinutes"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:ExpireMinutes en appsettings.json");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, rol.Nombre),
                // Claim privado para validar organizacion_id (OWASP API1)
                new Claim("organizacion_id", usuario.OrganizacionId?.ToString() ?? "0")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtExpireMinutes)),
                signingCredentials: credenciales
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Genera hash de refresh token criptográficamente seguro (OWASP API2).
        /// </summary>
        public string GenerateRefreshTokenHash()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        /// <summary>
        /// Valida que usuario tenga acceso a una organización (OWASP API1).
        /// Impide que Usuario Cliente acceda a otra organización.
        /// </summary>
        public bool ValidaAccesoOrganizacion(Usuario usuario, int organizacionIdSolicitada)
        {
            // Admin: acceso a todo
            if (usuario.Rol?.Nombre == "Administrador")
                return true;

            // Cliente: solo acceso a su propia organización
            if (usuario.Rol?.Nombre == "Cliente")
                return usuario.OrganizacionId == organizacionIdSolicitada;

            // Supervisor y Analista: acceso a su organizacion_id
            return usuario.OrganizacionId == organizacionIdSolicitada;
        }
    }
}

