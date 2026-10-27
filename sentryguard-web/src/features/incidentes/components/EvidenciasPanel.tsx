import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import type { Evidencia } from '../../../api/contracts'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState } from '../../../components/ErrorState'
import { FormField } from '../../../components/FormField'
import { ListadoResponsivo } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { formatearFecha } from '../../../utils/formato'
import { evidenciaFormSchema, type EvidenciaFormValues } from '../schema'
import { useAgregarEvidencia, useEvidencias } from '../queries'

interface Props {
  incidenteId: number
  puedeAgregar: boolean
}

// Evidencias del incidente (tb_EvidenciaIncidente). Se registra la referencia al archivo guardado en el
// repositorio de evidencias, no el archivo en si.
export function EvidenciasPanel({ incidenteId, puedeAgregar }: Props) {
  const consulta = useEvidencias(incidenteId)

  return (
    <section aria-labelledby="titulo-evidencias" className="panel">
      <h2 id="titulo-evidencias">Evidencias</h2>
      {consulta.isPending && <LoadingState mensaje="Cargando evidencias…" />}
      {consulta.isError && <ErrorState error={consulta.error} titulo="No se pudieron cargar las evidencias" onReintentar={() => consulta.refetch()} />}
      {consulta.data?.length === 0 && <EmptyState compacto titulo="Sin evidencias registradas" />}
      {consulta.data && consulta.data.length > 0 && (
        <ListadoResponsivo
          items={consulta.data}
          etiqueta="Evidencias del incidente"
          nivelTitulo={3}
          clave={(e) => e.id}
          principal={{ encabezado: 'Archivo', render: (e) => e.nombreArchivo }}
          columnas={[
            { encabezado: 'Ubicación', render: (e) => <UbicacionEvidencia evidencia={e} /> },
            { encabezado: 'Registrada por', render: (e) => e.subidoPor },
            { encabezado: 'Fecha', render: (e) => formatearFecha(e.fecha) },
          ]}
        />
      )}
      {puedeAgregar && <FormularioEvidencia incidenteId={incidenteId} />}
    </section>
  )
}

/**
 * Solo una URL https:// se muestra como enlace; cualquier otra cosa (ruta relativa o un valor raro que
 * hubiera llegado a la BD) se muestra como texto. Asi un "javascript:..." nunca termina en un href.
 */
function UbicacionEvidencia({ evidencia }: { evidencia: Evidencia }) {
  let url: URL | null = null
  try {
    url = new URL(evidencia.rutaAlmacenamiento)
  } catch {
    url = null
  }
  if (url?.protocol === 'https:') {
    return (
      <a href={url.href} target="_blank" rel="noopener noreferrer">
        {url.host}
        <span className="visualmente-oculto"> (se abre en una pestaña nueva)</span>
      </a>
    )
  }
  return <code>{evidencia.rutaAlmacenamiento}</code>
}

function FormularioEvidencia({ incidenteId }: { incidenteId: number }) {
  const agregar = useAgregarEvidencia(incidenteId)
  const { notificar } = useNotifications()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<EvidenciaFormValues>({ resolver: zodResolver(evidenciaFormSchema), defaultValues: { nombreArchivo: '', rutaAlmacenamiento: '' } })

  const enviar = handleSubmit(async (v) => {
    setErrorGeneral(null)
    try {
      await agregar.mutateAsync(v)
      reset()
      notificar('Evidencia registrada.')
    } catch (error) {
      setErrorGeneral(aplicarErroresApi(error, setError, ['nombreArchivo', 'rutaAlmacenamiento']))
    }
  })

  return (
    <form className="formulario formulario--compacto" onSubmit={enviar} noValidate aria-labelledby="titulo-nueva-evidencia">
      <h3 id="titulo-nueva-evidencia">Registrar evidencia</h3>
      <ResumenErrores errorGeneral={errorGeneral} cantidadErrores={Object.keys(errors).length} enviado={submitCount > 0} />
      <FormField label="Nombre del archivo" obligatorio error={errors.nombreArchivo?.message} ayuda="Por ejemplo: captura-firewall.png">
        <input type="text" autoComplete="off" maxLength={255} {...register('nombreArchivo')} />
      </FormField>
      <FormField
        label="Ubicación"
        obligatorio
        error={errors.rutaAlmacenamiento?.message}
        ayuda="URL https:// del repositorio de evidencias o ruta relativa, por ejemplo incidentes/12/captura.png"
      >
        <input type="text" autoComplete="off" inputMode="url" maxLength={1000} {...register('rutaAlmacenamiento')} />
      </FormField>
      <div className="acciones">
        <button type="submit" className="boton boton--primario" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando…' : 'Registrar evidencia'}
        </button>
      </div>
    </form>
  )
}
