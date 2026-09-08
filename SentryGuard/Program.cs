using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SentryGuard.Data;
using SentryGuard.Endpoints;
using SentryGuard.Middleware;
using SentryGuard.Repositories;
using SentryGuard.Services;
using System.Text;

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

// Add services to the container
builder.Services.AddOpenApi();
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

// Configure the HTTP request pipeline
// Middleware personalizado: Manejo de excepciones (primero para capturar todo)
app.UseGlobalExceptionHandling();

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
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// Mapear Endpoints (Minimal APIs)
app.MapUsuarioApi();
app.MapIncidenteApi();
app.MapOrganizacionApi();
app.MapActivoApi();
app.MapCatalogoApi();

app.Run();