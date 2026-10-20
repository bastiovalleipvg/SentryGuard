import { useQueryClient } from '@tanstack/react-query'
import { createContext, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import type { UsuarioActual } from '../../api/contracts'
import { registrarManejador401 } from '../../api/httpClient'
import { authService } from './authService'
import { tokenStorage } from './tokenStorage'

export type EstadoSesion = 'verificando' | 'autenticado' | 'anonimo'
export type MotivoCierre = 'expirada' | 'manual' | null

export interface AuthContextValue {
  estado: EstadoSesion
  usuario: UsuarioActual | null
  motivoCierre: MotivoCierre
  iniciarSesion: (email: string, password: string) => Promise<void>
  cerrarSesion: (motivo?: Exclude<MotivoCierre, null>) => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [estado, setEstado] = useState<EstadoSesion>(() => (tokenStorage.obtenerToken() ? 'verificando' : 'anonimo'))
  const [usuario, setUsuario] = useState<UsuarioActual | null>(null)
  const [motivoCierre, setMotivoCierre] = useState<MotivoCierre>(null)
  const temporizador = useRef<number | undefined>(undefined)

  const cerrarSesion = useCallback((motivo: Exclude<MotivoCierre, null> = 'manual') => {
    window.clearTimeout(temporizador.current)
    tokenStorage.limpiar()
    // Borra todo lo cacheado: el siguiente usuario del mismo navegador no debe ver datos del anterior.
    queryClient.clear()
    setUsuario(null)
    setMotivoCierre(motivo)
    setEstado('anonimo')
  }, [queryClient])

  // Cierra la sesion justo cuando vence el JWT, sin esperar a que la API responda 401.
  const programarExpiracion = useCallback(() => {
    window.clearTimeout(temporizador.current)
    const ms = tokenStorage.msHastaExpirar()
    if (ms !== null) temporizador.current = window.setTimeout(() => cerrarSesion('expirada'), Math.max(ms, 0))
  }, [cerrarSesion])

  useEffect(() => {
    registrarManejador401(() => cerrarSesion('expirada'))
    return () => registrarManejador401(null)
  }, [cerrarSesion])

  // Al recargar la pagina con un token guardado, el rol se pide de nuevo a la API (fuente de verdad).
  useEffect(() => {
    if (estado !== 'verificando') return
    let cancelado = false
    authService.obtenerUsuarioActual()
      .then((u) => {
        if (cancelado) return
        setUsuario(u)
        setEstado('autenticado')
        programarExpiracion()
      })
      .catch(() => {
        if (!cancelado) cerrarSesion('expirada')
      })
    return () => { cancelado = true }
  }, [estado, cerrarSesion, programarExpiracion])

  useEffect(() => () => window.clearTimeout(temporizador.current), [])

  const iniciarSesion = useCallback(async (email: string, password: string) => {
    const { token, expiresIn } = await authService.login(email, password)
    tokenStorage.guardar(token, expiresIn)
    try {
      const u = await authService.obtenerUsuarioActual()
      setUsuario(u)
      setMotivoCierre(null)
      setEstado('autenticado')
      programarExpiracion()
    } catch (error) {
      tokenStorage.limpiar()
      throw error
    }
  }, [programarExpiracion])

  const valor = useMemo<AuthContextValue>(
    () => ({ estado, usuario, motivoCierre, iniciarSesion, cerrarSesion }),
    [estado, usuario, motivoCierre, iniciarSesion, cerrarSesion],
  )

  return <AuthContext value={valor}>{children}</AuthContext>
}
