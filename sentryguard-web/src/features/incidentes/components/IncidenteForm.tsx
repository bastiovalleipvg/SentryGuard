import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { FormField } from '../../../components/FormField'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { useActivos } from '../../activos/activosService'
import { useSeveridades } from '../../catalogos/catalogosService'
import { LIMITES, incidenteFormSchema, type IncidenteFormValues, type ModoFormulario } from '../schema'

interface Props {
  modo: ModoFormulario
  valoresIniciales?: Partial<IncidenteFormValues>
  textoEnviar: string
  urlCancelar: string
  onEnviar: (valores: IncidenteFormValues) => Promise<void>
}

const CAMPOS = ['titulo', 'descripcion', 'severidadId', 'activoId'] as const

export function IncidenteForm({ modo, valoresIniciales, textoEnviar, urlCancelar, onEnviar }: Props) {
  const schema = useMemo(() => incidenteFormSchema(modo), [modo])
  const severidades = useSeveridades()
  const activos = useActivos(modo === 'crear')
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<IncidenteFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { titulo: '', descripcion: '', severidadId: '', activoId: '', ...valoresIniciales },
  })

  const enviar = handleSubmit(async (valores) => {
    setErrorGeneral(null)
    try {
      await onEnviar(valores)
    } catch (error) {
      setErrorGeneral(aplicarErroresApi(error, setError, CAMPOS))
    }
  })

  return (
    <form className="formulario" onSubmit={enviar} noValidate>
      <ResumenErrores errorGeneral={errorGeneral} cantidadErrores={Object.keys(errors).length} enviado={submitCount > 0} />

      <FormField label="Título" obligatorio error={errors.titulo?.message} ayuda={`Entre ${LIMITES.tituloMin} y ${LIMITES.tituloMax} caracteres.`}>
        <input type="text" autoComplete="off" maxLength={LIMITES.tituloMax} {...register('titulo')} />
      </FormField>

      <FormField label="Descripción" obligatorio error={errors.descripcion?.message} ayuda="Qué ocurrió, cuándo se detectó y qué impacto tiene.">
        <textarea rows={6} maxLength={LIMITES.descripcionMax} {...register('descripcion')} />
      </FormField>

      {modo === 'crear' && (
        <FormField label="Activo afectado" obligatorio error={errors.activoId?.message}>
          {/* key: al llegar las opciones el select se vuelve a montar y RHF le aplica el valor inicial. */}
          <select key={activos.isPending ? 'cargando' : 'listo'} disabled={activos.isPending} {...register('activoId')}>
            <option value="">{activos.isPending ? 'Cargando activos…' : 'Selecciona un activo'}</option>
            {activos.data?.map((a) => (
              <option key={a.id} value={String(a.id)}>
                {a.nombre} ({a.categoria})
              </option>
            ))}
          </select>
        </FormField>
      )}

      <FormField label="Severidad" obligatorio error={errors.severidadId?.message}>
        <select key={severidades.isPending ? 'cargando' : 'listo'} disabled={severidades.isPending} {...register('severidadId')}>
          <option value="">{severidades.isPending ? 'Cargando severidades…' : 'Selecciona la severidad'}</option>
          {severidades.data?.map((s) => (
            <option key={s.id} value={String(s.id)}>
              {s.nombre} (SLA {s.horasSLA} h)
            </option>
          ))}
        </select>
      </FormField>

      {(activos.isError || severidades.isError) && (
        <p className="campo__error">No se pudieron cargar las opciones del formulario. Recarga la página.</p>
      )}

      <div className="acciones">
        <Link to={urlCancelar} className="boton boton--secundario">
          Cancelar
        </Link>
        <button type="submit" className="boton boton--primario" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando…' : textoEnviar}
        </button>
      </div>
    </form>
  )
}
