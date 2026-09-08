using SentryGuard.Data;
using System.Diagnostics;

namespace SentryGuard.Middleware
{
    public class StructuredLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StructuredLoggingMiddleware> _logger;

        public StructuredLoggingMiddleware(RequestDelegate next, ILogger<StructuredLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, SentryGuardDbContext db)
        {
            var stopwatch = Stopwatch.StartNew();

            // Capturar método y ruta
            var method = context.Request.Method;
            var path = context.Request.Path.Value;
            var queryString = context.Request.QueryString.Value;

            // Log de request
            _logger.LogInformation(
                "Request iniciado | Método: {Method} | Ruta: {Path} | Query: {Query} | IP: {IP}",
                method, path, queryString, context.Connection.RemoteIpAddress);

            // Llamar al siguiente middleware
            await _next(context);

            stopwatch.Stop();

            // Log de response
            _logger.LogInformation(
                "Request completado | Método: {Method} | Ruta: {Path} | Status: {StatusCode} | Duración: {Duration}ms",
                method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);

            // Registrar en LogAuditoria si es mutación (POST, PUT, DELETE, PATCH)
            if (context.Request.Method != "GET" && context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                try
                {
                    var usuarioIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var ipOrigen = context.Connection.RemoteIpAddress?.ToString() ?? "DESCONOCIDA";

                    if (int.TryParse(usuarioIdClaim, out var usuarioId))
                    {
                        var logAuditoria = new Models.LogAuditoria
                        {
                            UsuarioId = usuarioId,
                            Accion = $"{method} {path}",
                            IpOrigen = ipOrigen,
                            Timestamp = DateTime.UtcNow
                        };

                        db.LogsAuditoria.Add(logAuditoria);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("No se pudo registrar en LogAuditoria: {Error}", ex.Message);
                }
            }
        }
    }

    public static class StructuredLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseStructuredLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StructuredLoggingMiddleware>();
        }
    }
}
