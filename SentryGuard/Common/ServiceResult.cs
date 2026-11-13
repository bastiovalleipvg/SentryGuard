using Microsoft.AspNetCore.Http;

namespace SentryGuard.Common
{
    /// <summary>
    /// Resultado desacoplado para la capa de servicios de negocio (Clean Architecture / ADR-002).
    /// Permite a los servicios retornar resultados tipados y errores de negocio sin acoplarse al protocolo HTTP,
    /// facilitando pruebas unitarias directas.
    /// </summary>
    public class ServiceResult<T>
    {
        public bool Success { get; }
        public T? Data { get; }
        public int StatusCode { get; }
        public string? Title { get; }
        public string? Detail { get; }
        public string? Codigo { get; }
        public IDictionary<string, string[]>? Errors { get; }

        private ServiceResult(T data, int statusCode = StatusCodes.Status200OK)
        {
            Success = true;
            Data = data;
            StatusCode = statusCode;
        }

        private ServiceResult(int statusCode, string title, string detail, string codigo, IDictionary<string, string[]>? errors = null)
        {
            Success = false;
            StatusCode = statusCode;
            Title = title;
            Detail = detail;
            Codigo = codigo;
            Errors = errors;
        }

        public static ServiceResult<T> Ok(T data) => new(data, StatusCodes.Status200OK);

        public static ServiceResult<T> Created(T data) => new(data, StatusCodes.Status201Created);

        public static ServiceResult<T> NotFound(string detail = "El recurso solicitado no fue encontrado.", string codigo = "RECURSO_NO_ENCONTRADO") =>
            new(StatusCodes.Status404NotFound, "Recurso no encontrado", detail, codigo);

        public static ServiceResult<T> Forbid(string detail = "No tienes permisos para realizar esta acción sobre este recurso.", string codigo = "SIN_PERMISO") =>
            new(StatusCodes.Status403Forbidden, "Acceso denegado", detail, codigo);

        public static ServiceResult<T> Unauthorized(string detail = "Debes iniciar sesión para realizar esta acción.", string codigo = "NO_AUTENTICADO") =>
            new(StatusCodes.Status401Unauthorized, "No autenticado", detail, codigo);

        public static ServiceResult<T> BadRequest(string detail, string codigo = "SOLICITUD_INVALIDA", IDictionary<string, string[]>? errors = null) =>
            new(StatusCodes.Status400BadRequest, "Solicitud inválida", detail, codigo, errors);

        public static ServiceResult<T> Validation(IDictionary<string, string[]> errors, string detail = "Uno o más campos no cumplen con las validaciones requeridas.", string codigo = "DATOS_INVALIDOS") =>
            new(StatusCodes.Status400BadRequest, "Error de validación", detail, codigo, errors);

        public static ServiceResult<T> Conflict(string detail, string codigo = "CONFLICTO") =>
            new(StatusCodes.Status409Conflict, "Conflicto", detail, codigo);

        public static ServiceResult<T> InvalidTransition(string detail = "La transición de estado solicitada no está permitida.", string codigo = "TRANSICION_INVALIDA") =>
            new(StatusCodes.Status409Conflict, "Transición no permitida", detail, codigo);

        public static ServiceResult<T> BadGateway(string detail = "El servicio externo de IA falló o no está disponible.", string codigo = "IA_ERROR_PROVEEDOR") =>
            new(StatusCodes.Status502BadGateway, "Servicio de IA no disponible", detail, codigo);

        public static ServiceResult<T> GatewayTimeout(string detail = "El servicio externo de IA excedió el tiempo límite de 15 segundos.", string codigo = "IA_TIEMPO_AGOTADO") =>
            new(StatusCodes.Status504GatewayTimeout, "Tiempo de espera de IA agotado", detail, codigo);

        public IResult ToHttpResult(string? createdUri = null)
        {
            if (Success)
            {
                if (StatusCode == StatusCodes.Status201Created && createdUri != null)
                    return Results.Created(createdUri, Data);

                if (StatusCode == StatusCodes.Status204NoContent)
                    return Results.NoContent();

                return Results.Ok(Data);
            }

            return ApiResults.Problem(
                statusCode: StatusCode,
                title: Title ?? "Error",
                detail: Detail ?? "Ocurrió un error",
                codigo: Codigo ?? "ERROR",
                errors: Errors
            );
        }
    }

    public record PagedResult<T>(int Total, int Page, int PageSize, int TotalPages, IEnumerable<T> Items);
}

