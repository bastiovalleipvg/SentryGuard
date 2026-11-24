import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link } from 'react-router'
import { isApiError } from '../../../api/apiError'
import type { ClasificacionIa } from '../../../api/contracts'
import { FormField } from '../../../components/FormField'
import { useNotifications } from '../../../components/Notifications'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { useActivos } from '../../activos/activosService'
import { useCategoriasIncidente, useSeveridades } from '../../catalogos/catalogosService'
import { useClasificarIncidente } from '../../ia/iaService'
import { LIMITES, incidenteFormSchema, type IncidenteFormValues, type ModoFormulario } from '../schema'
import { SeveridadBadge } from './Badges'

interface Props {
  modo: ModoFormulario
  valoresIniciales?: Partial<IncidenteFormValues>
  textoEnviar: string
  urlCancelar: string
  onEnviar: (valores: IncidenteFormValues) => Promise<void>
}

const CAMPOS = ['titulo', 'descripcion', 'severidadId', 'activoId', 'categoriaId'] as const

export function IncidenteForm({ modo, valoresIniciales, textoEnviar, urlCancelar, onEnviar }: Props) {
  const schema = useMemo(() => incidenteFormSchema(modo), [modo])
  const severidades = useSeveridades()
  const categorias = useCategoriasIncidente()
  const activos = useActivos(modo === 'crear')
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const { notificar } = useNotifications()

  const clasificarIa = useClasificarIncidente()
  const [sugerenciaIa, setSugerenciaIa] = useState<ClasificacionIa | null>(null)
  const [errorIa, setErrorIa] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    setValue,
    control,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<IncidenteFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { titulo: '', descripcion: '', severidadId: '', activoId: '', categoriaId: '', ...valoresIniciales },
  })

  const tituloVal = useWatch({ control, name: 'titulo' }) || ''
  const descripcionVal = useWatch({ control, name: 'descripcion' }) || ''
  const puedePedirIa = modo === 'crear' && tituloVal.trim().length >= 5 && descripcionVal.trim().length >= 10 && !clasificarIa.isPending

  const handleSugerirIa = async () => {
    setErrorIa(null)
    try {
      const res = await clasificarIa.mutateAsync({
        titulo: tituloVal.trim(),
        descripcion: descripcionVal.trim(),
      })
      setSugerenciaIa(res)
    } catch (err) {
      if (isApiError(err) && err.status === 504) {
        setErrorIa('El servicio de IA no respondió a tiempo (tiempo de espera agotado después de 15 segundos). Puedes seleccionar la severidad de forma manual.')
      } else if (isApiError(err) && err.status === 502) {
        setErrorIa('El servicio de IA no está disponible en este momento. Puedes seleccionar la severidad de forma manual.')
      } else {
        setErrorIa('El servicio de IA no respondió a tiempo. Puedes seleccionar la severidad de forma manual.')
      }
    }
  }

  const handleAplicarSugerencia = () => {
    if (!sugerenciaIa) return
    setValue('severidadId', String(sugerenciaIa.severidadSugeridaId), { shouldValidate: true })
    // Si la categoria sugerida existe en el catalogo, tambien se completa
    const categoria = categorias.data?.find((c) => c.nombre.toLowerCase() === sugerenciaIa.categoriaSugerida.trim().toLowerCase())
    if (categoria) setValue('categoriaId', String(categoria.id), { shouldValidate: true })
    notificar('Sugerencia de IA aplicada a severidad y categoría.', 'exito')
  }

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
        <section className="panel-ia" aria-label="Asistente de clasificación con IA" aria-live="polite">
          <div className="panel-ia__cabecera">
            <div>
              <h3 className="panel-ia__titulo">✨ Asistente de Clasificación IA</h3>
              <p className="panel-ia__descripcion">
                Analiza el título y la descripción del incidente para sugerir severidad, categoría y acciones de contención inmediatas.
              </p>
            </div>
            <button
              type="button"
              className="boton boton--secundario"
              onClick={handleSugerirIa}
              disabled={!puedePedirIa}
              title={
                tituloVal.trim().length < 5 || descripcionVal.trim().length < 10
                  ? 'Ingresa al menos 5 caracteres en el título y 10 en la descripción para analizar'
                  : 'Solicitar sugerencia de clasificación a la IA'
              }
            >
              {clasificarIa.isPending ? 'Analizando incidente con IA…' : '✨ Sugerir clasificación con IA'}
            </button>
          </div>

          {errorIa && (
            <div className="aviso" style={{ marginTop: '0.75rem' }} role="status">
              {errorIa}
            </div>
          )}

          {sugerenciaIa && (
            <div className="panel-ia__resultado" style={{ marginTop: '0.75rem' }}>
              <div className="panel-ia__meta">
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                  <span>Severidad sugerida:</span>
                  <SeveridadBadge severidad={sugerenciaIa.severidadNombre} />
                  <span style={{ color: 'var(--texto-suave)' }}>•</span>
                  <span><strong>Categoría:</strong> {sugerenciaIa.categoriaSugerida}</span>
                  {sugerenciaIa.origen && (
                    <>
                      <span style={{ color: 'var(--texto-suave)' }}>•</span>
                      <span style={{ fontSize: '0.8rem', padding: '0.1rem 0.5rem', background: 'var(--fondo-suave, #f1f5f9)', borderRadius: '4px' }}>
                        {sugerenciaIa.origen === 'simulado' ? 'Sugerencia generada por el asistente simulado' : `Modelo: ${sugerenciaIa.origen}`}
                      </span>
                    </>
                  )}
                </div>
                <button
                  type="button"
                  className="boton boton--primario"
                  style={{ minHeight: '2.25rem', padding: '0.25rem 0.75rem', fontSize: '0.875rem' }}
                  onClick={handleAplicarSugerencia}
                >
                  Aplicar sugerencia a severidad
                </button>
              </div>
              <p style={{ margin: '0.5rem 0', fontSize: '0.9rem' }}>
                <em>{sugerenciaIa.justificacion}</em>
              </p>
              {sugerenciaIa.accionesRecomendadas?.length > 0 && (
                <div style={{ marginTop: '0.5rem' }}>
                  <strong style={{ fontSize: '0.85rem' }}>Acciones recomendadas:</strong>
                  <ul style={{ margin: '0.25rem 0 0 1.25rem', padding: 0, fontSize: '0.85rem' }}>
                    {sugerenciaIa.accionesRecomendadas.map((accion, i) => (
                      <li key={i}>{accion}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </section>
      )}

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

      <FormField label="Categoría" error={errors.categoriaId?.message} ayuda="Opcional. Clasifica el incidente para los reportes.">
        <select key={categorias.isPending ? 'cargando' : 'listo'} disabled={categorias.isPending} {...register('categoriaId')}>
          <option value="">{categorias.isPending ? 'Cargando categorías…' : 'Sin categoría'}</option>
          {categorias.data?.map((c) => (
            <option key={c.id} value={String(c.id)}>
              {c.nombre}
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

