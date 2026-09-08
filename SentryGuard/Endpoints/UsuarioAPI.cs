using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.Models;
using SentryGuard.Repositories;
using SentryGuard.Services;
using System.Security.Claims;

namespace SentryGuard.Endpoints
{
    public static class UsuarioAPI
    {
        public static void MapUsuarioApi(this WebApplication app)
        {
            var usuarios = app.MapGroup("/api/v1/usuarios")
                .WithTags("Usuarios");

            // Solo Administrador aprovisiona cuentas (evita que cualquiera se auto-asigne un rol, ver OWASP API5).
            usuarios.MapPost("/registro", RegistroUsuario).WithName("Registro")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            // Rate limiting por IP para mitigar fuerza bruta sobre credenciales (OWASP API4).
            usuarios.MapPost("/login", LoginUsuario).WithName("Login")
                .RequireRateLimiting("login");
            usuarios.MapGet("/me", ObtenerUsuarioActual).RequireAuthorization();
            usuarios.MapGet("/{id}", ObtenerUsuarioId).RequireAuthorization();
            usuarios.MapPost("/refresh-token", RefrescarToken).RequireAuthorization();
        }

        private static async Task<IResult> RegistroUsuario(RegistroRequest request, IUsuarioRepository usuarioRepository, SentryGuardDbContext db, AuthService authService)
        {
            var usuarioExistente = await usuarioRepository.GetByEmailAsync(request.Email);
            if (usuarioExistente != null)
                return Results.BadRequest(new { error = "Email ya registrado" });

            var rol = await db.Roles.FindAsync(request.RolId);
            if (rol == null)
                return Results.BadRequest(new { error = "Rol no existe" });

            var usuario = new Usuario { Nombre = request.Nombre, Email = request.Email, RolId = request.RolId, OrganizacionId = request.OrganizacionId, Activo = true };
            usuario.PasswordHash = authService.HashPassword(usuario, request.Password);

            await usuarioRepository.AddAsync(usuario);
            return Results.Created($"/api/v1/usuarios/{usuario.Id}", new { id = usuario.Id, nombre = usuario.Nombre, email = usuario.Email, rol = rol.Nombre });
        }

        private static async Task<IResult> LoginUsuario(LoginRequest login, SentryGuardDbContext db, AuthService authService)
        {
            var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Email == login.Email);
            if (usuario == null || !usuario.Activo)
                return Results.Unauthorized();

            var (esValida, razon) = await authService.VerifyPasswordAsync(usuario, login.Password, db);
            if (!esValida)
                return Results.Json(new { error = razon }, statusCode: StatusCodes.Status401Unauthorized);

            var token = authService.GenerateJwtToken(usuario, usuario.Rol!);

            var refreshTokenHash = authService.GenerateRefreshTokenHash();
            var refreshToken = new RefreshToken { UsuarioId = usuario.Id, TokenHash = refreshTokenHash, FechaEmision = DateTime.UtcNow, FechaExpira = DateTime.UtcNow.AddDays(7) };

            await db.RefreshTokens.AddAsync(refreshToken);
            await db.SaveChangesAsync();

            return Results.Ok(new { token, refreshToken = refreshTokenHash, expiresIn = 3600 });
        }

        private static async Task<IResult> ObtenerUsuarioActual(HttpContext httpContext, SentryGuardDbContext db)
        {
            var usuarioId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioId, out var id))
                return Results.Unauthorized();

            var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
                return Results.NotFound();

            return Results.Ok(new { id = usuario.Id, nombre = usuario.Nombre, email = usuario.Email, rol = usuario.Rol?.Nombre, activo = usuario.Activo });
        }

        private static async Task<IResult> ObtenerUsuarioId(int id, HttpContext httpContext, SentryGuardDbContext db)
        {
            var usuarioActualId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;

            if (rol != "Administrador" && usuarioActualId != id.ToString())
                return Results.Forbid();

            var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
                return Results.NotFound();

            return Results.Ok(new { id = usuario.Id, nombre = usuario.Nombre, email = usuario.Email, rolId = usuario.RolId, rol = usuario.Rol?.Nombre, organizacionId = usuario.OrganizacionId, activo = usuario.Activo });
        }

        private static async Task<IResult> RefrescarToken(HttpContext httpContext, RefreshTokenRequest request, SentryGuardDbContext db, AuthService authService)
        {
            var usuarioId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioId, out var id))
                return Results.Unauthorized();

            var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UsuarioId == id && rt.TokenHash == request.RefreshToken && !rt.Revocado);
            if (refreshToken == null || refreshToken.FechaExpira < DateTime.UtcNow)
                return Results.Unauthorized();

            var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
                return Results.Problem("Error: Usuario no encontrado");

            var nuevoToken = authService.GenerateJwtToken(usuario, usuario.Rol!);
            return Results.Ok(new { token = nuevoToken });
        }

        record RegistroRequest(string Nombre, string Email, string Password, int RolId, int? OrganizacionId);
        record LoginRequest(string Email, string Password);
        record RefreshTokenRequest(string RefreshToken);
    }
}


