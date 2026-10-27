import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { isApiError } from '../api/apiError'
import { mensajeDeError } from './ErrorState'

/**
 * Lleva los errores de validacion de la API (400 con { errors: { campo: [...] } }) a cada campo del
 * formulario, enfoca el primero y devuelve el mensaje general para el resumen.
 */
export function aplicarErroresApi<T extends FieldValues>(error: unknown, setError: UseFormSetError<T>, campos: readonly Path<T>[]): string {
  if (isApiError(error) && Object.keys(error.fieldErrors).length > 0) {
    let primero = true
    let alguno = false
    for (const [campo, mensaje] of Object.entries(error.fieldErrors)) {
      if ((campos as readonly string[]).includes(campo)) {
        setError(campo as Path<T>, { type: 'server', message: mensaje }, { shouldFocus: primero })
        primero = false
        alguno = true
      }
    }
    // Un error de un campo que no esta en el formulario se muestra como mensaje general.
    if (!alguno) return Object.values(error.fieldErrors)[0]
    return 'El servidor rechazó algunos datos. Revisa los campos marcados.'
  }
  return mensajeDeError(error)
}

interface Props {
  errorGeneral: string | null
  cantidadErrores: number
  enviado: boolean
}

/** Resumen al inicio del formulario, dentro de una region role="alert" que ya existe al cargar (asi se anuncia). */
export function ResumenErrores({ errorGeneral, cantidadErrores, enviado }: Props) {
  const mostrar = errorGeneral || (enviado && cantidadErrores > 0)
  return (
    <div role="alert" className="resumen-errores-contenedor">
      {mostrar && (
        <div className="resumen-errores">
          <p>
            {errorGeneral ?? `Revisa ${cantidadErrores === 1 ? 'el campo marcado' : `los ${cantidadErrores} campos marcados`} antes de continuar.`}
          </p>
        </div>
      )}
    </div>
  )
}
