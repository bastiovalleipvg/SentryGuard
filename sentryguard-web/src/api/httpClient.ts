import type { z } from 'zod'
import { env } from '../config/env'
import { tokenStorage } from '../features/auth/tokenStorage'
import { ApiError } from './apiError'

type Metodo = 'GET' | 'POST' | 'PUT' | 'DELETE'
type Query = Record<string, string | number | undefined | null>

interface Opciones<T> {
  method?: Metodo
  body?: unknown
  query?: Query
  signal?: AbortSignal
  /** Esquema con el que se valida la respuesta antes de devolverla. */
  schema?: z.ZodType<T>
  /** false para endpoints publicos (login): no adjunta el token ni dispara el cierre por 401. */
  auth?: boolean
}

let alRecibir401: (() => void) | null = null

/** El AuthProvider registra aqui como reaccionar a un 401 (token expirado o invalido). */
export function registrarManejador401(handler: (() => void) | null): void {
  alRecibir401 = handler
}

function construirUrl(ruta: string, query?: Query): URL {
  const url = new URL(ruta, env.apiUrl)
  for (const [clave, valor] of Object.entries(query ?? {})) {
    if (valor !== undefined && valor !== null && valor !== '') url.searchParams.set(clave, String(valor))
  }
  return url
}

/**
 * Unico punto de salida hacia la API: arma la URL a partir de VITE_API_URL, adjunta el JWT como
 * Bearer, normaliza los errores en ApiError y valida la forma de la respuesta.
 */
export async function apiRequest<T = unknown>(ruta: string, opciones: Opciones<T> = {}): Promise<T> {
  const { method = 'GET', body, query, signal, schema, auth = true } = opciones

  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  const token = auth ? tokenStorage.obtenerToken() : null
  if (token) headers.Authorization = `Bearer ${token}`

  let response: Response
  try {
    response = await fetch(construirUrl(ruta, query), {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
      // Sin cookies: la autenticacion va solo en la cabecera Authorization, por eso CSRF no aplica.
      credentials: 'omit',
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    // fetch falla igual ante red caida o bloqueo CORS; el navegador no expone la diferencia.
    throw ApiError.network()
  }

  if (!response.ok) {
    if (response.status === 401 && auth) alRecibir401?.()
    throw await ApiError.fromResponse(response)
  }

  if (response.status === 204) return undefined as T

  let datos: unknown = undefined
  const texto = await response.text()
  if (texto) {
    try {
      datos = JSON.parse(texto)
    } catch {
      throw ApiError.invalidResponse()
    }
  }

  if (!schema) return datos as T
  const resultado = schema.safeParse(datos)
  if (!resultado.success) throw ApiError.invalidResponse()
  return resultado.data
}

/**
 * Descarga un archivo generado por la API (exportacion de reportes): adjunta el Bearer, recibe un Blob
 * y lo entrega con el nombre que indica Content-Disposition. Los errores salen como ApiError.
 */
export async function descargarArchivo(ruta: string, query?: Query, signal?: AbortSignal): Promise<{ blob: Blob; nombre: string }> {
  const token = tokenStorage.obtenerToken()
  let response: Response
  try {
    response = await fetch(construirUrl(ruta, query), {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      signal,
      credentials: 'omit',
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw ApiError.network()
  }
  if (!response.ok) {
    if (response.status === 401) alRecibir401?.()
    throw await ApiError.fromResponse(response)
  }
  const disposicion = response.headers.get('Content-Disposition') ?? ''
  const nombre = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposicion)?.[1] ?? 'reporte'
  return { blob: await response.blob(), nombre: decodeURIComponent(nombre) }
}
