using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key en appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Issuer en appsettings.json");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Audience en appsettings.json");

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Rate limiting basico por IP en endpoints sensibles a fuerza bruta / abuso (OWASP API4)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
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
                [new OpenApiSecuritySchemeReference(BearerSchemeId, context.Document, null)] = new List<string>()
            });
        }
        return Task.CompletedTask;
    });
});
builder.Services.AddScoped<AuthService>();

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

app.UseRateLimiter();

// Mapear Endpoints (Minimal APIs)
app.MapUsuarioApi();
app.MapIncidenteApi();
app.MapOrganizacionApi();
app.MapActivoApi();
app.MapCatalogoApi();

app.Run();