import { useEffect, useId, useRef } from 'react'

interface Props {
  abierto: boolean
  titulo: string
  mensaje: string
  textoConfirmar: string
  procesando?: boolean
  onConfirmar: () => void
  onCancelar: () => void
}

/**
 * Dialogo de confirmacion con <dialog> nativo y showModal(): el navegador atrapa el foco dentro,
 * vuelve inerte el resto de la pagina y cierra con Escape. Al abrir enfoca "Cancelar" (la opcion
 * no destructiva) y al cerrar devuelve el foco al boton que lo abrio.
 */
export function ConfirmDialog({ abierto, titulo, mensaje, textoConfirmar, procesando = false, onConfirmar, onCancelar }: Props) {
  const ref = useRef<HTMLDialogElement>(null)
  const cancelarRef = useRef<HTMLButtonElement>(null)
  const foco = useRef<HTMLElement | null>(null)
  const idTitulo = useId()
  const idMensaje = useId()

  useEffect(() => {
    const dialogo = ref.current
    if (!dialogo) return
    if (abierto && !dialogo.open) {
      foco.current = document.activeElement as HTMLElement | null
      dialogo.showModal()
      cancelarRef.current?.focus()
    } else if (!abierto && dialogo.open) {
      dialogo.close()
      foco.current?.focus()
    }
  }, [abierto])

  return (
    <dialog
      ref={ref}
      className="dialogo"
      aria-labelledby={idTitulo}
      aria-describedby={idMensaje}
      onCancel={(evento) => {
        evento.preventDefault()
        if (!procesando) onCancelar()
      }}
    >
      <h2 id={idTitulo} className="dialogo__titulo">{titulo}</h2>
      <p id={idMensaje}>{mensaje}</p>
      <div className="acciones">
        <button ref={cancelarRef} type="button" className="boton boton--secundario" onClick={onCancelar} disabled={procesando}>
          Cancelar
        </button>
        <button type="button" className="boton boton--peligro" onClick={onConfirmar} disabled={procesando}>
          {procesando ? 'Procesando…' : textoConfirmar}
        </button>
      </div>
    </dialog>
  )
}
