interface Props {
  mensaje?: string
}

export function LoadingState({ mensaje = 'Cargando…' }: Props) {
  return (
    <div className="estado estado--carga" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <span>{mensaje}</span>
    </div>
  )
}
