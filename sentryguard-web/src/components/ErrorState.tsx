import { isApiError } from '../api/apiError'
import { Icono } from './Icono'

interface Props {
  error: unknown
  titulo?: string
  onReintentar?: () => void
}

export function mensajeDeError(error: unknown): string {
  if (isApiError(error)) return error.message
  return 'Ocurrió un error inesperado. Recarga la página e inténtalo de nuevo.'
}

export function ErrorState({ error, titulo = 'No se pudo cargar la información', onReintentar }: Props) {
  return (
    <div className="estado estado--error" role="alert">
      <Icono nombre="error" className="estado__icono" />
      <h2 className="estado__titulo">{titulo}</h2>
      <p>{mensajeDeError(error)}</p>
      {onReintentar && (
        <button type="button" className="boton boton--secundario" onClick={onReintentar}>
          Reintentar
        </button>
      )}
    </div>
  )
}
