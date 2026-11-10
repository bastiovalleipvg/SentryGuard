using System.Text.Json;

namespace SentryGuard.Middleware
{
    /// <summary>
    /// Captura cualquier excepcion no controlada y responde con un JSON generico (OWASP API8).
    /// El detalle real (mensaje, stack trace) solo se escribe en el log del servidor; al cliente
    /// se le entrega un traceId para correlacionar, nunca el mensaje interno de la excepcion.
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción no manejada | TraceId: {TraceId} | Ruta: {Path}",
                    context.TraceIdentifier, context.Request.Path);

                if (context.Response.HasStarted)
                    throw;

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (status, title, codigo, mensaje) = exception switch
            {
                // JSON mal formado, tipos incorrectos o parametros de ruta invalidos.
                BadHttpRequestException bad => (bad.StatusCode, "Solicitud inválida", "SOLICITUD_INVALIDA", "La solicitud no tiene un formato válido."),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acceso denegado", "ACCESO_DENEGADO", "No tienes permisos para realizar esta acción."),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", "RECURSO_NO_ENCONTRADO", "El recurso solicitado no existe."),
                _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor", "ERROR_INTERNO", "Ocurrió un error inesperado. Si persiste, contacta a soporte indicando el traceId.")
            };

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var response = new ErrorResponse
            {
                Type = $"https://sentryguard.local/errores/{codigo.ToLowerInvariant().Replace('_', '-')}",
                Title = title,
                Status = status,
                Detail = mensaje,
                Codigo = codigo,
                Mensaje = mensaje,
                TraceId = context.TraceIdentifier
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return context.Response.WriteAsync(jsonResponse);
        }

        public class ErrorResponse
        {
            public string Type { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public int Status { get; set; }
            public string Detail { get; set; } = string.Empty;
            public string Codigo { get; set; } = "ERROR";
            public string Mensaje { get; set; } = "Ocurrió un error";
            public string? TraceId { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        }
    }

    public static class GlobalExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }
    }
}
