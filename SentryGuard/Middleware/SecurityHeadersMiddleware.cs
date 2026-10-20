namespace SentryGuard.Middleware
{
    /// <summary>
    /// Agrega headers de seguridad HTTP estandar a toda respuesta (OWASP API8).
    /// HSTS se maneja aparte con el middleware nativo de ASP.NET Core (app.UseHsts()).
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // La API solo devuelve JSON: una CSP que no permite cargar nada impide que una
            // respuesta se interprete como documento ejecutable. Scalar (solo en Development)
            // necesita sus propios scripts, por eso se excluye.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                headers["Cache-Control"] = "no-store";
            }

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
