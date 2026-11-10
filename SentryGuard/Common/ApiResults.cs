using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SentryGuard.Common
{
    /// <summary>
    /// Fábrica centralizada de respuestas de error basadas en el estándar ProblemDetails (RFC 9457 / RFC 7807).
    /// Garantiza que toda la API devuelva una estructura coherente con las extensiones 'codigo' y 'traceId'.
    /// </summary>
    public static class ApiResults
    {
        private const string BaseTypeUrl = "https://sentryguard.local/errores/";

        public static string CurrentTraceId =>
            Activity.Current?.Id ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        public static IResult Problem(
            int statusCode,
            string title,
            string detail,
            string codigo,
            string? typeSlug = null,
            IDictionary<string, string[]>? errors = null)
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = $"{BaseTypeUrl}{typeSlug ?? codigo.ToLowerInvariant().Replace('_', '-')}"
            };

            problem.Extensions["codigo"] = codigo;
            problem.Extensions["mensaje"] = detail; // Compatibilidad directa con clientes que lean 'mensaje'
            problem.Extensions["traceId"] = CurrentTraceId;

            if (errors is { Count: > 0 })
            {
                problem.Extensions["errors"] = errors;
            }

            return Results.Problem(problem);
        }

        public static IResult Validacion(
            IDictionary<string, string[]> errors,
            string detail = "Uno o más campos no cumplen con las validaciones requeridas.",
            string codigo = "DATOS_INVALIDOS")
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Error de validación",
                detail: detail,
                codigo: codigo,
                typeSlug: "validacion",
                errors: errors
            );
        }

        public static IResult SolicitudInvalida(
            string detail = "La solicitud no tiene un formato válido.",
            string codigo = "SOLICITUD_INVALIDA")
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: detail,
                codigo: codigo,
                typeSlug: "solicitud-invalida"
            );
        }

        public static IResult NoAutenticado(
            string detail = "Debes iniciar sesión para acceder a este recurso.",
            string codigo = "NO_AUTENTICADO")
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No autenticado",
                detail: detail,
                codigo: codigo,
                typeSlug: "no-autenticado"
            );
        }

        public static IResult SinPermiso(
            string detail = "No tienes permisos para realizar esta acción.",
            string codigo = "SIN_PERMISO")
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Acceso denegado",
                detail: detail,
                codigo: codigo,
                typeSlug: "sin-permiso"
            );
        }

        public static IResult NoEncontrado(
            string detail = "El recurso solicitado no fue encontrado.",
            string codigo = "RECURSO_NO_ENCONTRADO")
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Recurso no encontrado",
                detail: detail,
                codigo: codigo,
                typeSlug: "no-encontrado"
            );
        }

        public static IResult Conflicto(
            string detail,
            string codigo = "CONFLICTO")
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto con el estado actual",
                detail: detail,
                codigo: codigo,
                typeSlug: "conflicto"
            );
        }

        public static IResult TransicionInvalida(
            string detail = "La transición de estado solicitada no está permitida.",
            string codigo = "TRANSICION_INVALIDA",
            IDictionary<string, string[]>? errors = null)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Transición de estado no permitida",
                detail: detail,
                codigo: codigo,
                typeSlug: "transicion-invalida",
                errors: errors
            );
        }

        public static IResult ErrorServicioExterno(
            string detail = "El servicio externo no respondió o devolvió un error.",
            string codigo = "ERROR_SERVICIO_EXTERNO")
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Error en servicio externo",
                detail: detail,
                codigo: codigo,
                typeSlug: "error-servicio-externo"
            );
        }

        public static IResult TiempoAgotado(
            string detail = "La operación superó el tiempo máximo de espera permitido.",
            string codigo = "TIEMPO_AGOTADO")
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Tiempo de espera agotado",
                detail: detail,
                codigo: codigo,
                typeSlug: "tiempo-agotado"
            );
        }

        public static IResult ErrorInterno(
            string detail = "Ocurrió un error inesperado en el servidor.",
            string codigo = "ERROR_INTERNO")
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error interno del servidor",
                detail: detail,
                codigo: codigo,
                typeSlug: "error-interno"
            );
        }
    }
}

