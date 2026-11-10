export type FieldErrors = Record<string, string>

const MENSAJES_POR_ESTADO: Record<number, string> = {
  0: 'No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.',
  400: 'Revisa los datos ingresados.',
  401: 'Tu sesión expiró o no es válida. Inicia sesión nuevamente.',
  403: 'No tienes permisos para realizar esta acción.',
  404: 'El recurso solicitado no existe o fue eliminado.',
  409: 'El recurso cambió mientras lo editabas. Recarga e inténtalo de nuevo.',
  429: 'Demasiadas solicitudes. Espera un momento antes de volver a intentarlo.',
  500: 'Ocurrió un error en el servidor. Inténtalo más tarde.',
}

/**
 * Error normalizado para cualquier respuesta no exitosa de la API. Los componentes solo
 * trabajan con este tipo: nunca leen cuerpos crudos ni muestran detalles tecnicos.
 */
export class ApiError extends Error {
  readonly status: number
  readonly codigo: string
  readonly fieldErrors: FieldErrors
  readonly retryAfterSeconds: number | null

  constructor(status: number, codigo: string, message: string, fieldErrors: FieldErrors = {}, retryAfterSeconds: number | null = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.codigo = codigo
    this.fieldErrors = fieldErrors
    this.retryAfterSeconds = retryAfterSeconds
  }

  static network(): ApiError {
    return new ApiError(0, 'SIN_CONEXION', MENSAJES_POR_ESTADO[0])
  }

  static invalidResponse(): ApiError {
    return new ApiError(502, 'RESPUESTA_INVALIDA', 'La respuesta del servidor no tiene el formato esperado.')
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    const cuerpo = await leerJsonSeguro(response)
    const status = response.status
    const fieldErrors = extraerErroresDeCampo(cuerpo)
    const retryAfter = Number(response.headers.get('Retry-After'))

    // Lee 'detail' (estándar RFC 9457 ProblemDetails) o 'mensaje' (formato propio/compatibilidad)
    const mensajeApi = typeof cuerpo?.detail === 'string'
      ? cuerpo.detail
      : typeof cuerpo?.mensaje === 'string'
        ? cuerpo.mensaje
        : null
    const codigo = typeof cuerpo?.codigo === 'string' ? cuerpo.codigo : `HTTP_${status}`
    const mensaje = mensajeApi ?? MENSAJES_POR_ESTADO[status] ?? MENSAJES_POR_ESTADO[status >= 500 ? 500 : 400]

    return new ApiError(status, codigo, mensaje, fieldErrors, Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null)
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}

async function leerJsonSeguro(response: Response): Promise<Record<string, unknown> | null> {
  try {
    const texto = await response.text()
    if (!texto) return null
    const valor: unknown = JSON.parse(texto)
    return valor !== null && typeof valor === 'object' ? (valor as Record<string, unknown>) : null
  } catch {
    return null
  }
}

// Formato ValidationProblem de ASP.NET Core: { errors: { campo: ["mensaje", ...] } }
function extraerErroresDeCampo(cuerpo: Record<string, unknown> | null): FieldErrors {
  const errores = cuerpo?.errors
  if (!errores || typeof errores !== 'object') return {}

  const resultado: FieldErrors = {}
  for (const [campo, mensajes] of Object.entries(errores as Record<string, unknown>)) {
    if (Array.isArray(mensajes) && typeof mensajes[0] === 'string') {
      const clave = campo.charAt(0).toLowerCase() + campo.slice(1)
      resultado[clave] = mensajes[0]
    }
  }
  return resultado
}
