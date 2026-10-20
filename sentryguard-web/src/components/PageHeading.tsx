import { useEffect, useRef, type ReactNode } from 'react'
import { useLocation } from 'react-router'

// Clave de la ubicacion con la que se mostro el primer encabezado de esta carga de la app. Se usa
// la clave de React Router (y no un booleano en un efecto) porque StrictMode ejecuta los efectos dos
// veces en desarrollo, y porque al recargar el navegador restaura la clave guardada en history.state.
let claveCargaInicial: string | null = null

interface Props {
  children: ReactNode
  /** Texto para la pestaña del navegador; por defecto, el mismo del encabezado. */
  tituloDocumento?: string
}

/**
 * Encabezado principal de cada vista. En cada cambio de ruta recibe el foco para que los lectores
 * de pantalla anuncien la nueva pagina (en una SPA el navegador no lo hace solo) y actualiza
 * document.title (WCAG 2.4.2).
 */
export function PageHeading({ children, tituloDocumento }: Props) {
  const ref = useRef<HTMLHeadingElement>(null)
  const { key } = useLocation()
  const titulo = tituloDocumento ?? (typeof children === 'string' ? children : '')
  claveCargaInicial ??= key

  useEffect(() => {
    document.title = titulo ? `${titulo} · SentryGuard` : 'SentryGuard'
  }, [titulo])

  useEffect(() => {
    // En la carga inicial el foco queda al inicio del documento (el primer Tab llega al enlace
    // "Saltar al contenido"); en cada navegacion posterior se mueve al encabezado de la vista.
    if (key !== claveCargaInicial) ref.current?.focus()
  }, [key])

  return (
    <h1 ref={ref} tabIndex={-1} className="titulo-pagina">
      {children}
    </h1>
  )
}
