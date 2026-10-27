import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { FormField } from '../../../components/FormField'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { usePlanes } from '../../catalogos/catalogosService'
import { organizacionFormSchema, type OrganizacionFormEntrada, type OrganizacionFormValues } from '../schema'

interface Props {
  valoresIniciales?: OrganizacionFormEntrada
  textoEnviar: string
  urlCancelar: string
  onEnviar: (valores: OrganizacionFormValues) => Promise<void>
}

const CAMPOS = ['razonSocial', 'rut', 'planId'] as const

export function OrganizacionForm({ valoresIniciales, textoEnviar, urlCancelar, onEnviar }: Props) {
  const schema = useMemo(() => organizacionFormSchema(valoresIniciales?.rut), [valoresIniciales?.rut])
  const planes = usePlanes()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<OrganizacionFormEntrada, unknown, OrganizacionFormValues>({
    resolver: zodResolver(schema),
    defaultValues: valoresIniciales ?? { razonSocial: '', rut: '', planId: '' },
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

      <FormField label="Razón social" obligatorio error={errors.razonSocial?.message}>
        <input type="text" autoComplete="organization" maxLength={200} {...register('razonSocial')} />
      </FormField>

      <FormField label="RUT" obligatorio error={errors.rut?.message} ayuda="Con guion y dígito verificador, por ejemplo 76123456-0. Los puntos se quitan solos.">
        <input type="text" autoComplete="off" spellCheck={false} maxLength={12} {...register('rut')} />
      </FormField>

      <FormField label="Plan contratado" obligatorio error={errors.planId?.message}>
        <select key={planes.isPending ? 'cargando' : 'listo'} disabled={planes.isPending} {...register('planId')}>
          <option value="">{planes.isPending ? 'Cargando planes…' : 'Selecciona un plan'}</option>
          {planes.data?.map((p) => <option key={p.id} value={String(p.id)}>{p.nombre} (respuesta base {p.tiempoRespuestaBaseH} h)</option>)}
        </select>
      </FormField>

      {planes.isError && <p className="campo__error">No se pudieron cargar los planes. Recarga la página.</p>}

      <div className="acciones">
        <Link to={urlCancelar} className="boton boton--secundario">Cancelar</Link>
        <button type="submit" className="boton boton--primario" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando…' : textoEnviar}
        </button>
      </div>
    </form>
  )
}
