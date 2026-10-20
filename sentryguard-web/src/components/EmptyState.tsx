import type { ReactNode } from 'react'

interface Props {
  titulo: string
  children?: ReactNode
}

export function EmptyState({ titulo, children }: Props) {
  return (
    <div className="estado estado--vacio">
      <h2 className="estado__titulo">{titulo}</h2>
      {children}
    </div>
  )
}
