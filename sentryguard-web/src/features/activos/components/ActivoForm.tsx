import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { FormField } from '../../../components/FormField'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { useCategorias } from '../../catalogos/catalogosService'
import type { ModoFormulario } from '../../incidentes/schema'
import { useOpcionesOrganizaciones } from '../../organizaciones/organizacionesService'
import { activoFormSchema, LIMITES_ACTIVO, type ActivoFormValues } from '../schema'

interface Props {
  modo: ModoFormulario
  valoresIniciales?: Partial<ActivoFormValues>
  textoEnviar: string
  urlCancelar: string
  onEnviar: (valores: ActivoFormValues) => Promise<void>
}

const CAMPOS = ['nombre', 'hostnameIp', 'categoriaId', 'organizacionId'] as const

export function ActivoForm({ modo, valoresIniciales, textoEnviar, urlCancelar, onEnviar }: Props) {
  const schema = useMemo(() => activoFormSchema(modo), [modo])
  const categorias = useCategorias()
  const organizaciones = useOpcionesOrganizaciones(modo === 'crear')
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<ActivoFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { nombre: '', hostnameIp: '', categoriaId: '', organizacionId: '', ...valoresIniciales },
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

      <FormField label="Nombre" obligatorio error={errors.nombre?.message} ayuda={`Entre ${LIMITES_ACTIVO.nombreMin} y ${LIMITES_ACTIVO.nombreMax} caracteres.`}>
        <input type="text" autoComplete="off" maxLength={LIMITES_ACTIVO.nombreMax} {...register('nombre')} />
      </FormField>

      <FormField label="Hostname o IP" error={errors.hostnameIp?.message} ayuda="Opcional. Por ejemplo: srv-web01.empresa.cl o 10.10.0.10">
        <input type="text" autoComplete="off" spellCheck={false} maxLength={LIMITES_ACTIVO.hostMax} {...register('hostnameIp')} />
      </FormField>

      <FormField label="Categoría" obligatorio error={errors.categoriaId?.message}>
        {/* key: al llegar las opciones el select se vuelve a montar y RHF le aplica el valor inicial. */}
        <select key={categorias.isPending ? 'cargando' : 'listo'} disabled={categorias.isPending} {...register('categoriaId')}>
          <option value="">{categorias.isPending ? 'Cargando categorías…' : 'Selecciona una categoría'}</option>
          {categorias.data?.map((c) => <option key={c.id} value={String(c.id)}>{c.nombre}</option>)}
        </select>
      </FormField>

      {modo === 'crear' && (
        <FormField label="Organización" obligatorio error={errors.organizacionId?.message}>
          <select key={organizaciones.isPending ? 'cargando' : 'listo'} disabled={organizaciones.isPending} {...register('organizacionId')}>
            <option value="">{organizaciones.isPending ? 'Cargando organizaciones…' : 'Selecciona una organización'}</option>
            {organizaciones.data?.map((o) => <option key={o.id} value={String(o.id)}>{o.razonSocial} ({o.rut})</option>)}
          </select>
        </FormField>
      )}

      {(categorias.isError || organizaciones.isError) && (
        <p className="campo__error">No se pudieron cargar las opciones del formulario. Recarga la página.</p>
      )}

      <div className="acciones">
        <Link to={urlCancelar} className="boton boton--secundario">Cancelar</Link>
        <button type="submit" className="boton boton--primario" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando…' : textoEnviar}
        </button>
      </div>
    </form>
  )
}
