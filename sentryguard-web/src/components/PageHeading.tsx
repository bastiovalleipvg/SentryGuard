import { useEffect, useRef, type ReactNode } from 'react'

let esPrimeraCarga = true

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
  const titulo = tituloDocumento ?? (typeof children === 'string' ? children : '')

  useEffect(() => {
    document.title = titulo ? `${titulo} · SentryGuard` : 'SentryGuard'
  }, [titulo])

  useEffect(() => {
    // En la carga inicial el foco queda al inicio del documento (comportamiento normal del navegador).
    if (esPrimeraCarga) {
      esPrimeraCarga = false
      return
    }
    ref.current?.focus()
  }, [])

  return (
    <h1 ref={ref} tabIndex={-1} className="titulo-pagina">
      {children}
    </h1>
  )
}
