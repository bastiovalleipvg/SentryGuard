import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { isApiError } from '../../../api/apiError'
import { FormField } from '../../../components/FormField'
import { mensajeDeError } from '../../../components/ErrorState'
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

const CAMPOS: (keyof IncidenteFormValues)[] = ['titulo', 'descripcion', 'severidadId', 'activoId']

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
      // Errores de validacion de la API (400): se muestran junto a cada campo afectado.
      if (isApiError(error) && Object.keys(error.fieldErrors).length > 0) {
        let primero = true
        for (const [campo, mensaje] of Object.entries(error.fieldErrors)) {
          if ((CAMPOS as string[]).includes(campo)) {
            setError(campo as keyof IncidenteFormValues, { type: 'server', message: mensaje }, { shouldFocus: primero })
            primero = false
          }
        }
        setErrorGeneral('El servidor rechazó algunos datos. Revisa los campos marcados.')
        return
      }
      setErrorGeneral(mensajeDeError(error))
    }
  })

  const cantidadErrores = Object.keys(errors).length

  return (
    <form className="formulario" onSubmit={enviar} noValidate>
      <div role="alert" className="resumen-errores-contenedor">
        {(errorGeneral || (submitCount > 0 && cantidadErrores > 0)) && (
          <div className="resumen-errores">
            <p>
              {errorGeneral ?? `Revisa ${cantidadErrores === 1 ? 'el campo marcado' : `los ${cantidadErrores} campos marcados`} antes de continuar.`}
            </p>
          </div>
        )}
      </div>

      <FormField label="Título" obligatorio error={errors.titulo?.message} ayuda={`Entre ${LIMITES.tituloMin} y ${LIMITES.tituloMax} caracteres.`}>
        <input type="text" autoComplete="off" maxLength={LIMITES.tituloMax} {...register('titulo')} />
      </FormField>

      <FormField label="Descripción" obligatorio error={errors.descripcion?.message} ayuda="Qué ocurrió, cuándo se detectó y qué impacto tiene.">
        <textarea rows={6} maxLength={LIMITES.descripcionMax} {...register('descripcion')} />
      </FormField>

      {modo === 'crear' && (
        <FormField label="Activo afectado" obligatorio error={errors.activoId?.message}>
          <select disabled={activos.isPending} {...register('activoId')}>
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
        <select disabled={severidades.isPending} {...register('severidadId')}>
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
