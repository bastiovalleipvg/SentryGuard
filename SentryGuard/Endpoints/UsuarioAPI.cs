using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.DTOs;
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

            // Solo Administrador aprovisiona y administra cuentas (evita que cualquiera se auto-asigne un rol, ver OWASP API5).
            usuarios.MapPost("/registro", RegistroUsuario).WithName("Registro")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            usuarios.MapGet("", ListarUsuarios).WithName("Listar usuarios")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            usuarios.MapPut("/{id}", ActualizarUsuario).WithName("Actualizar usuario")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            // Rate limiting por IP para mitigar fuerza bruta sobre credenciales (OWASP API4).
            usuarios.MapPost("/login", LoginUsuario).WithName("Login")
                .RequireRateLimiting("login");
            usuarios.MapGet("/me", ObtenerUsuarioActual).RequireAuthorization();
            // Lista minima (id, nombre) para el selector de asignacion; solo quien puede asignar la necesita.
            usuarios.MapGet("/analistas", ListarAnalistas)
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            usuarios.MapGet("/{id}", ObtenerUsuarioId).RequireAuthorization();
            usuarios.MapPost("/refresh-token", RefrescarToken).RequireAuthorization();
        }

        // Reglas comunes de alta y edicion: un Cliente pertenece a una organizacion; el staff de CyberOps no.
        private static async Task ValidarRolYOrganizacionAsync(Dictionary<string, string[]> errores, SentryGuardDbContext db, int rolId, int? organizacionId)
        {
            var rol = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rolId);
            if (rol == null)
            {
                errores["rolId"] = ["El rol seleccionado no existe."];
                return;
            }

            if (rol.Nombre == "Cliente")
            {
                if (!organizacionId.HasValue)
                    errores["organizacionId"] = ["Un usuario Cliente debe pertenecer a una organización."];
                else if (!await db.Organizaciones.AnyAsync(o => o.Id == organizacionId.Value))
                    errores["organizacionId"] = ["La organización seleccionada no existe."];
            }
            else if (organizacionId.HasValue)
                errores["organizacionId"] = ["Solo los usuarios Cliente pertenecen a una organización."];
        }

        private static async Task<IResult> RegistroUsuario(RegistroRequest request, IUsuarioRepository usuarioRepository, SentryGuardDbContext db, AuthService authService)
        {
            var errores = new Dictionary<string, string[]>();
            Validacion.Texto(errores, "nombre", request.Nombre, 3, 150, requerido: true);
            if (!Validacion.EsEmail(request.Email))
                errores["email"] = ["Ingresa un correo válido."];
            else if (await usuarioRepository.GetByEmailAsync(request.Email!.Trim()) != null)
                errores["email"] = ["Ya existe un usuario con ese correo."];
            var errorPassword = Validacion.ErrorPassword(request.Password);
            if (errorPassword != null) errores["password"] = [errorPassword];
            await ValidarRolYOrganizacionAsync(errores, db, request.RolId, request.OrganizacionId);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var usuario = new Usuario
            {
                Nombre = request.Nombre!.Trim(),
                Email = request.Email!.Trim(),
                RolId = request.RolId,
                OrganizacionId = request.OrganizacionId,
                Activo = true
            };
            usuario.PasswordHash = authService.HashPassword(usuario, request.Password!);

            await usuarioRepository.AddAsync(usuario);
            var rol = await db.Roles.AsNoTracking().FirstAsync(r => r.Id == request.RolId);
            return Results.Created($"/api/v1/usuarios/{usuario.Id}", new { id = usuario.Id, nombre = usuario.Nombre, email = usuario.Email, rol = rol.Nombre });
        }

        private static async Task<IResult> ListarUsuarios(SentryGuardDbContext db, int page = 1, int pageSize = 10, string? q = null, int? rolId = null, int? organizacionId = null)
        {
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);

            IQueryable<Usuario> query = db.Usuarios;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                if (termino.Length > 100) termino = termino[..100];
                query = query.Where(u => u.Nombre.Contains(termino) || u.Email.Contains(termino));
            }
            if (rolId.HasValue) query = query.Where(u => u.RolId == rolId.Value);
            if (organizacionId.HasValue) query = query.Where(u => u.OrganizacionId == organizacionId.Value);

            var total = await query.CountAsync();
            // Proyeccion a DTO: el password_hash nunca sale del servidor (OWASP API3).
            var items = await query
                .OrderBy(u => u.Nombre)
                .ThenBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UsuarioListDTO
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    RolId = u.RolId,
                    Rol = u.Rol!.Nombre,
                    OrganizacionId = u.OrganizacionId,
                    Organizacion = u.Organizacion != null ? u.Organizacion.RazonSocial : null,
                    Activo = u.Activo
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize), items });
        }

        // No hay DELETE: un usuario con bitacora o evidencias se desactiva (Activo = false) para conservar la trazabilidad.
        private static async Task<IResult> ActualizarUsuario(int id, ActualizarUsuarioRequest request, HttpContext httpContext, SentryGuardDbContext db)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return Results.NotFound();

            var errores = new Dictionary<string, string[]>();
            Validacion.Texto(errores, "nombre", request.Nombre, 3, 150, requerido: true);
            await ValidarRolYOrganizacionAsync(errores, db, request.RolId, request.OrganizacionId);

            // El administrador no puede quitarse a si mismo el acceso ni el rol (dejaria el sistema sin administrador).
            var esUnoMismo = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value == id.ToString();
            if (esUnoMismo && !request.Activo)
                errores["activo"] = ["No puedes desactivar tu propia cuenta."];
            if (esUnoMismo && request.RolId != usuario.RolId)
                errores["rolId"] = ["No puedes cambiar tu propio rol."];
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            usuario.Nombre = request.Nombre!.Trim();
            usuario.RolId = request.RolId;
            usuario.OrganizacionId = request.OrganizacionId;
            usuario.Activo = request.Activo;
            await db.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        // Mismo mensaje para email inexistente, contraseña incorrecta o cuenta bloqueada: un mensaje
        // distinto por caso permitiria enumerar cuentas validas (OWASP API2).
        private static IResult CredencialesInvalidas() => Results.Json(
            new
            {
                type = "https://sentryguard.local/errores/credenciales-invalidas",
                title = "Credenciales inválidas",
                status = StatusCodes.Status401Unauthorized,
                detail = "Correo o contraseña incorrectos, o la cuenta está bloqueada temporalmente.",
                codigo = "CREDENCIALES_INVALIDAS",
                mensaje = "Correo o contraseña incorrectos, o la cuenta está bloqueada temporalmente."
            },
            contentType: "application/problem+json",
            statusCode: StatusCodes.Status401Unauthorized);

        private static async Task<IResult> LoginUsuario(LoginRequest login, SentryGuardDbContext db, AuthService authService, IConfiguration configuration)
        {
            var errores = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(login.Email) || login.Email.Length > 320)
                errores["email"] = ["Ingresa un correo válido."];
            if (string.IsNullOrEmpty(login.Password) || login.Password.Length > 200)
                errores["password"] = ["Ingresa tu contraseña."];
            if (errores.Count > 0)
                return Results.ValidationProblem(errores);

            var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Email == login.Email!.Trim());
            if (usuario == null || !usuario.Activo)
                return CredencialesInvalidas();

            var (esValida, _) = await authService.VerifyPasswordAsync(usuario, login.Password!, db);
            if (!esValida)
                return CredencialesInvalidas();

            var token = authService.GenerateJwtToken(usuario, usuario.Rol!);

            // En la base de datos solo se guarda el hash SHA-256; al cliente se le entrega el token en claro (OWASP API2).
            var rawRefreshToken = authService.GenerateRefreshToken();
            var refreshTokenHash = authService.HashearRefreshToken(rawRefreshToken);
            var refreshToken = new RefreshToken
            {
                UsuarioId = usuario.Id,
                TokenHash = refreshTokenHash,
                FechaEmision = DateTime.UtcNow,
                FechaExpira = DateTime.UtcNow.AddDays(7)
            };

            await db.RefreshTokens.AddAsync(refreshToken);
            await db.SaveChangesAsync();

            var expiraEnSegundos = configuration.GetValue("Jwt:ExpireMinutes", 60) * 60;
            return Results.Ok(new { token, refreshToken = rawRefreshToken, expiresIn = expiraEnSegundos });
        }

        private static async Task<IResult> ObtenerUsuarioActual(HttpContext httpContext, SentryGuardDbContext db)
        {
            var usuarioId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioId, out var id))
                return Results.Unauthorized();

            var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null || !usuario.Activo)
                return Results.Unauthorized();

            return Results.Ok(new { id = usuario.Id, nombre = usuario.Nombre, email = usuario.Email, rol = usuario.Rol?.Nombre, organizacionId = usuario.OrganizacionId, activo = usuario.Activo });
        }

        private static async Task<IResult> ListarAnalistas(SentryGuardDbContext db)
        {
            var analistas = await db.Usuarios
                .Where(u => u.Activo && u.Rol!.Nombre == "Analista")
                .OrderBy(u => u.Nombre)
                .Select(u => new { id = u.Id, nombre = u.Nombre })
                .ToListAsync();
            return Results.Ok(analistas);
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

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.Unauthorized();

            var incomingHash = authService.HashearRefreshToken(request.RefreshToken);
            var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UsuarioId == id && rt.TokenHash == incomingHash && !rt.Revocado);
            if (refreshToken == null || refreshToken.FechaExpira < DateTime.UtcNow)
                return Results.Unauthorized();

            var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null || !usuario.Activo)
                return Results.Unauthorized();

            var nuevoToken = authService.GenerateJwtToken(usuario, usuario.Rol!);
            return Results.Ok(new { token = nuevoToken });
        }

        record RegistroRequest(string? Nombre, string? Email, string? Password, int RolId, int? OrganizacionId);
        record ActualizarUsuarioRequest(string? Nombre, int RolId, int? OrganizacionId, bool Activo);
        record LoginRequest(string? Email, string? Password);
        record RefreshTokenRequest(string RefreshToken);
    }
}


