import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState } from '../../../components/ErrorState'
import { FormField } from '../../../components/FormField'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { formatearFecha } from '../../../utils/formato'
import { bitacoraFormSchema, type BitacoraFormValues } from '../schema'
import { useAgregarBitacora, useBitacora } from '../queries'

interface Props {
  incidenteId: number
  puedeRegistrar: boolean
}

// Historial del incidente (tb_BitacoraIncidente): entradas automaticas del flujo y notas del equipo.
export function BitacoraPanel({ incidenteId, puedeRegistrar }: Props) {
  const consulta = useBitacora(incidenteId)

  return (
    <section aria-labelledby="titulo-bitacora" className="panel">
      <h2 id="titulo-bitacora">Bitácora</h2>
      {puedeRegistrar && <FormularioBitacora incidenteId={incidenteId} />}

      <h3>Historial</h3>
      {consulta.isPending && <LoadingState mensaje="Cargando bitácora…" />}
      {consulta.isError && <ErrorState error={consulta.error} titulo="No se pudo cargar la bitácora" onReintentar={() => consulta.refetch()} />}
      {consulta.data?.length === 0 && <EmptyState titulo="Sin entradas en la bitácora" />}
      {consulta.data && consulta.data.length > 0 && (
        <ol className="linea-tiempo" aria-label="Entradas de la bitácora, de la más reciente a la más antigua">
          {consulta.data.map((entrada) => (
            <li key={entrada.id} className="linea-tiempo__item">
              <p className="linea-tiempo__accion">{entrada.accion}</p>
              {/* Texto escrito por usuarios: se renderiza como texto, nunca como HTML. */}
              {entrada.comentario && <p className="texto-largo">{entrada.comentario}</p>}
              <p className="linea-tiempo__meta">
                {entrada.usuario} · <time dateTime={`${entrada.timestamp}Z`}>{formatearFecha(entrada.timestamp)}</time>
              </p>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}

function FormularioBitacora({ incidenteId }: { incidenteId: number }) {
  const agregar = useAgregarBitacora(incidenteId)
  const { notificar } = useNotifications()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<BitacoraFormValues>({ resolver: zodResolver(bitacoraFormSchema), defaultValues: { accion: '', comentario: '' } })

  const enviar = handleSubmit(async (v) => {
    setErrorGeneral(null)
    try {
      await agregar.mutateAsync({ accion: v.accion, comentario: v.comentario || null })
      reset()
      notificar('Entrada agregada a la bitácora.')
    } catch (error) {
      setErrorGeneral(aplicarErroresApi(error, setError, ['accion', 'comentario']))
    }
  })

  return (
    <form className="formulario formulario--compacto" onSubmit={enviar} noValidate aria-labelledby="titulo-nueva-entrada">
      <h3 id="titulo-nueva-entrada">Nueva entrada</h3>
      <ResumenErrores errorGeneral={errorGeneral} cantidadErrores={Object.keys(errors).length} enviado={submitCount > 0} />
      <FormField label="Acción realizada" obligatorio error={errors.accion?.message} ayuda="Por ejemplo: «Se aisló el equipo de la red».">
        <input type="text" autoComplete="off" maxLength={100} {...register('accion')} />
      </FormField>
      <FormField label="Comentario" error={errors.comentario?.message} ayuda="Opcional. Hasta 2000 caracteres.">
        <textarea rows={3} maxLength={2000} {...register('comentario')} />
      </FormField>
      <div className="acciones">
        <button type="submit" className="boton boton--primario" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando…' : 'Agregar a la bitácora'}
        </button>
      </div>
    </form>
  )
}
