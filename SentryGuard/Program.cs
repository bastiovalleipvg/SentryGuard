using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using SentryGuard.Data;
using SentryGuard.Endpoints;
using SentryGuard.Middleware;
using SentryGuard.Repositories;
using SentryGuard.Services;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configurar JWT
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta configurar Jwt:Key (usa dotnet user-secrets en Development).");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException("Falta configurar Jwt:Issuer.");

var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Falta configurar Jwt:Audience.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            // Mitigacion de algoritmo JWT (OWASP API2): no aceptar tokens firmados con otro algoritmo o con 'none'.
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };
    });

builder.Services.AddAuthorization();

// CORS restringido a los origenes conocidos del frontend (OWASP API8)
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithExposedHeaders("Location", "Retry-After");
    });
});

// Rate limiting basico por IP en endpoints sensibles a fuerza bruta / abuso (OWASP API4)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var segundos = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : 60;
        context.HttpContext.Response.Headers.RetryAfter = segundos.ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            codigo = "DEMASIADAS_SOLICITUDES",
            mensaje = $"Demasiados intentos. Espera {segundos} segundos antes de volver a intentarlo.",
            reintentarEnSegundos = segundos
        }, cancellationToken);
    };

    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy("incidentes-crear", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// Add services to the container
const string BearerSchemeId = "Bearer";
builder.Services.AddOpenApi(options =>
{
    // Declara el esquema Bearer en el documento OpenAPI para que Scalar muestre el boton "Authorize".
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.SecuritySchemes[BearerSchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Pega aqui el token JWT obtenido en POST /api/v1/usuarios/login (sin el prefijo 'Bearer ')."
        };
        return Task.CompletedTask;
    });

    // Exige el esquema Bearer solo en los endpoints que ya tienen [Authorize]/.RequireAuthorization().
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var requiereAuth = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
        if (requiereAuth)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = BearerSchemeId
                    }
                }] = Array.Empty<string>()
            });
        }
        return Task.CompletedTask;
    });
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IIncidenteService, IncidenteService>();
builder.Services.AddScoped<IActivoService, ActivoService>();
builder.Services.AddScoped<IOrganizacionService, OrganizacionService>();
builder.Services.AddScoped<GeneradorDatosSinteticos>();

// Registrar Repositorios (Scoped)
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IIncidenteRepository, IncidenteRepository>();
builder.Services.AddScoped<IOrganizacionRepository, OrganizacionRepository>();
builder.Services.AddScoped<IActivoRepository, ActivoRepository>();

// Agregar BD
builder.Services.AddDbContext<SentryGuardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Ejecución directa de generador de datos sintéticos vía CLI (ADR-009 / Contenedor seed)
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<GeneradorDatosSinteticos>();
    await seeder.GenerarAsync();
    return;
}

// Headers de seguridad en toda respuesta, incluidas las de error (OWASP API8)
app.UseSecurityHeaders();

// HSTS solo fuera de Development (en dev no hay certificado HTTPS de confianza)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Configure the HTTP request pipeline
// Middleware personalizado: Manejo de excepciones (primero para capturar todo)
app.UseGlobalExceptionHandling();

// CORS: debe ir antes de autenticacion/autorizacion
app.UseCors("Frontend");

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Middleware de logging estructurado
app.UseStructuredLogging();

// OpenAPI/Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("SentryGuard CyberOps API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .AddPreferredSecuritySchemes([BearerSchemeId])
            .EnablePersistentAuthentication();
    });
}

app.UseHttpsRedirection();

// Rate limiting deshabilitado en tests de integracion: todas las requests de
// WebApplicationFactory salen de la misma IP simulada y activarian el limite de /login.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}

// Mapear Endpoints (Minimal APIs)
app.MapUsuarioApi();
app.MapIncidenteApi();
app.MapOrganizacionApi();
app.MapActivoApi();
app.MapCatalogoApi();
app.MapAuditoriaApi();

// Endpoint administrativo para disparo manual de datos sintéticos (ADR-009)
app.MapPost("/api/v1/admin/seed-datos", async (GeneradorDatosSinteticos seeder) =>
{
    var total = await seeder.GenerarAsync();
    return Results.Ok(new { mensaje = "Generación de datos sintéticos completada con éxito.", totalIncidentes = total });
})
.RequireAuthorization(policy => policy.RequireRole("Administrador"))
.WithName("SeedDatosSinteticos")
.WithTags("Administración")
.WithSummary("Ejecuta el generador idempotente de datos sintéticos (ADR-009)");

app.Run();

// Necesario para que WebApplicationFactory<Program> (tests de integracion) pueda referenciar
// el entry point generado por top-level statements.
public partial class Program { }