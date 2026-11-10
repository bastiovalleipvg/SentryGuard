using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Falta configurar Jwt:Key (usa dotnet user-secrets en Development).");

            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var jwtExpireMinutes = _configuration["Jwt:ExpireMinutes"];
            if (string.IsNullOrWhiteSpace(jwtExpireMinutes))
                throw new InvalidOperationException("Falta configurar Jwt:ExpireMinutes en appsettings.json");

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
        /// Genera token de refresco en texto plano criptográficamente seguro (OWASP API2).
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Calcula el hash SHA-256 del refresh token (OWASP API2).
        /// En la base de datos solo se almacena este hash, nunca el token en texto plano.
        /// </summary>
        public string HashearRefreshToken(string token)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Genera hash de refresh token criptográficamente seguro (compatibilidad hacia atrás).
        /// </summary>
        public string GenerateRefreshTokenHash()
        {
            return HashearRefreshToken(GenerateRefreshToken());
        }

        /// <summary>
        /// Valida que usuario tenga acceso a una organización (OWASP API1).
        /// Impide que Usuario Cliente acceda a otra organización.
        /// Administrador y Supervisor son staff interno de CyberOps: no están atados a una
        /// organización cliente, tienen acceso a todas. Analista no se valida por organización
        /// sino por incidente asignado (ver ValidaAccesoIncidente).
        /// </summary>
        public bool ValidaAccesoOrganizacion(Usuario usuario, int organizacionIdSolicitada)
        {
            if (usuario.Rol?.Nombre is "Administrador" or "Supervisor")
                return true;

            if (usuario.Rol?.Nombre == "Cliente")
                return usuario.OrganizacionId == organizacionIdSolicitada;

            return false;
        }

        /// <summary>
        /// Valida que usuario tenga acceso a un incidente puntual (OWASP API1).
        /// Administrador y Supervisor: acceso total. Cliente: solo incidentes de su propia
        /// organización. Analista: solo incidentes que tiene asignados.
        /// </summary>
        public bool ValidaAccesoIncidente(Usuario usuario, Incidente incidente)
        {
            if (usuario.Rol?.Nombre is "Administrador" or "Supervisor")
                return true;

            if (usuario.Rol?.Nombre == "Cliente")
                return usuario.OrganizacionId == incidente.OrganizacionId;

            if (usuario.Rol?.Nombre == "Analista")
                return incidente.AnalistaAsignadoId == usuario.Id;

            return false;
        }

        /// <summary>
        /// Obtiene el usuario autenticado (con su Rol cargado) a partir del claim del JWT.
        /// Centraliza la lectura del NameIdentifier para no repetirla en cada endpoint.
        /// </summary>
        public async Task<Usuario?> ObtenerUsuarioAutenticadoAsync(HttpContext httpContext, SentryGuardDbContext db)
        {
            var usuarioId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioId, out var id))
                return null;

            // Filtra u.Activo para que un usuario desactivado pierda el acceso de inmediato
            // aun cuando su JWT siga temporalmente vigente (OWASP API2).
            return await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id && u.Activo);
        }
    }
}

