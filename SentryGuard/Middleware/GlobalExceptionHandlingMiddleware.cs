using System.Text.Json;

namespace SentryGuard.Middleware
{
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
                _logger.LogError(ex, "Excepción no manejada | Mensaje: {Message} | Ruta: {Path}",
                    ex.Message, context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case ArgumentNullException argNullEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Codigo = "ARGUMENTO_NULO";
                    response.Mensaje = "Un argumento requerido es nulo";
                    response.Detalle = argNullEx.ParamName;
                    break;

                case ArgumentException argEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Codigo = "ARGUMENTO_INVALIDO";
                    response.Mensaje = "Argumento inválido";
                    response.Detalle = argEx.Message;
                    break;

                case InvalidOperationException invOpEx:
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    response.Codigo = "OPERACION_INVALIDA";
                    response.Mensaje = "Operación inválida";
                    response.Detalle = invOpEx.Message;
                    break;

                case UnauthorizedAccessException uaEx:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response.Codigo = "NO_AUTORIZADO";
                    response.Mensaje = "No autorizado para realizar esta acción";
                    response.Detalle = uaEx.Message;
                    break;

                case KeyNotFoundException knfEx:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response.Codigo = "RECURSO_NO_ENCONTRADO";
                    response.Mensaje = "El recurso solicitado no existe";
                    response.Detalle = knfEx.Message;
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Codigo = "ERROR_INTERNO";
                    response.Mensaje = "Error interno del servidor";
                    response.Detalle = exception.Message;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return context.Response.WriteAsync(jsonResponse);
        }

        public class ErrorResponse
        {
            public string Codigo { get; set; } = "ERROR";
            public string Mensaje { get; set; } = "Ocurrió un error";
            public string? Detalle { get; set; }
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
