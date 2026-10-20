import { createContext, use, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'

type Tono = 'exito' | 'error'

interface Notificacion {
  id: number
  texto: string
  tono: Tono
}

interface NotificationsContextValue {
  notificar: (texto: string, tono?: Tono) => void
}

const NotificationsContext = createContext<NotificationsContextValue | null>(null)

const DURACION_MS = 7000

/**
 * Mensajes de resultado ("Incidente creado", errores de una accion). Las dos regiones aria-live
 * existen siempre en el DOM para que los lectores de pantalla anuncien cada mensaje nuevo
 * (WCAG 4.1.3 Mensajes de estado): los exitos en "polite" y los errores en "assertive".
 */
export function NotificationsProvider({ children }: { children: ReactNode }) {
  const [actual, setActual] = useState<Notificacion | null>(null)
  const contador = useRef(0)

  const notificar = useCallback((texto: string, tono: Tono = 'exito') => {
    contador.current += 1
    setActual({ id: contador.current, texto, tono })
  }, [])

  useEffect(() => {
    if (!actual) return
    const t = window.setTimeout(() => setActual(null), DURACION_MS)
    return () => window.clearTimeout(t)
  }, [actual])

  const valor = useMemo(() => ({ notificar }), [notificar])

  return (
    <NotificationsContext value={valor}>
      {children}
      <section className="notificaciones" aria-label="Notificaciones">
        <div role="status" aria-live="polite">
          {actual?.tono === 'exito' && (
            <p key={actual.id} className="notificacion notificacion--exito">
              <span aria-hidden="true">✓ </span>{actual.texto}
            </p>
          )}
        </div>
        <div role="alert" aria-live="assertive">
          {actual?.tono === 'error' && (
            <p key={actual.id} className="notificacion notificacion--error">
              <span aria-hidden="true">⚠ </span>{actual.texto}
            </p>
          )}
        </div>
      </section>
    </NotificationsContext>
  )
}

export function useNotifications(): NotificationsContextValue {
  const contexto = use(NotificationsContext)
  if (!contexto) throw new Error('useNotifications debe usarse dentro de <NotificationsProvider>.')
  return contexto
}
