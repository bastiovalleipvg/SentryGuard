import { cloneElement, useId, type ReactElement, type ReactNode } from 'react'

interface Props {
  label: string
  error?: string
  ayuda?: string
  obligatorio?: boolean
  /** Un unico control (input, select o textarea); recibe id y atributos ARIA automaticamente. */
  children: ReactElement<Record<string, unknown>>
  extra?: ReactNode
}

/**
 * Asocia etiqueta, ayuda y error a un control: <label for>, aria-describedby y aria-invalid
 * (WCAG 1.3.1, 3.3.1, 3.3.2). La obligatoriedad se indica con texto, no solo con color o un asterisco.
 */
export function FormField({ label, error, ayuda, obligatorio = false, children, extra }: Props) {
  const id = useId()
  const idAyuda = `${id}-ayuda`
  const idError = `${id}-error`
  const describedBy = [ayuda ? idAyuda : null, error ? idError : null].filter(Boolean).join(' ') || undefined

  return (
    <div className={`campo${error ? ' campo--error' : ''}`}>
      <label htmlFor={id} className="campo__etiqueta">
        {label}
        {obligatorio && <span className="campo__obligatorio"> (obligatorio)</span>}
      </label>
      {ayuda && (
        <p id={idAyuda} className="campo__ayuda">
          {ayuda}
        </p>
      )}
      {cloneElement(children, {
        id,
        'aria-describedby': describedBy,
        'aria-invalid': error ? true : undefined,
        'aria-required': obligatorio || undefined,
      })}
      {error && (
        <p id={idError} className="campo__error">
          <span aria-hidden="true">⚠ </span>
          {error}
        </p>
      )}
      {extra}
    </div>
  )
}
