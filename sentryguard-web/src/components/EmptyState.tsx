import type { ReactNode } from 'react'
import { Icono } from './Icono'

interface Props {
  titulo: string
  children?: ReactNode
  /** Dentro de un panel que ya tiene su h2: el titulo va como texto destacado, no como otro encabezado. */
  compacto?: boolean
}

export function EmptyState({ titulo, children, compacto = false }: Props) {
  return (
    <div className={`estado estado--vacio${compacto ? ' estado--compacto' : ''}`}>
      <Icono nombre="vacio" className="estado__icono" />
      {compacto ? <p className="estado__titulo">{titulo}</p> : <h2 className="estado__titulo">{titulo}</h2>}
      {children}
    </div>
  )
}
