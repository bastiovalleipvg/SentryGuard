import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link } from 'react-router'
import { FormField } from '../../../components/FormField'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { useRoles } from '../../catalogos/catalogosService'
import type { ModoFormulario } from '../../incidentes/schema'
import { useOpcionesOrganizaciones } from '../../organizaciones/organizacionesService'
import { REQUISITOS_PASSWORD, usuarioFormSchema, type UsuarioFormValues } from '../schema'

interface Props {
  modo: ModoFormulario
  valoresIniciales?: Partial<UsuarioFormValues>
  /** El administrador editando su propia cuenta: no puede cambiarse el rol ni desactivarse. */
  esCuentaPropia?: boolean
  textoEnviar: string
  urlCancelar: string
  onEnviar: (valores: UsuarioFormValues, esCliente: boolean) => Promise<void>
}

const CAMPOS = ['nombre', 'email', 'password', 'rolId', 'organizacionId', 'activo'] as const

export function UsuarioForm({ modo, valoresIniciales, esCuentaPropia = false, textoEnviar, urlCancelar, onEnviar }: Props) {
  const roles = useRoles()
  const organizaciones = useOpcionesOrganizaciones(true)
  const idRolCliente = roles.data?.find((r) => r.nombre === 'Cliente')?.id
  const schema = useMemo(() => usuarioFormSchema(modo, idRolCliente), [modo, idRolCliente])
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    control,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<UsuarioFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { nombre: '', email: '', password: '', confirmacion: '', rolId: '', organizacionId: '', activo: true, ...valoresIniciales },
  })

  const rolSeleccionado = useWatch({ control, name: 'rolId' })
  const esCliente = idRolCliente !== undefined && Number(rolSeleccionado) === idRolCliente

  const enviar = handleSubmit(async (valores) => {
    setErrorGeneral(null)
    try {
      await onEnviar(valores, esCliente)
    } catch (error) {
      setErrorGeneral(aplicarErroresApi(error, setError, CAMPOS))
    }
  })

  return (
    <form className="formulario" onSubmit={enviar} noValidate>
      <ResumenErrores errorGeneral={errorGeneral} cantidadErrores={Object.keys(errors).length} enviado={submitCount > 0} />

      <FormField label="Nombre completo" obligatorio error={errors.nombre?.message}>
        <input type="text" autoComplete="off" maxLength={150} {...register('nombre')} />
      </FormField>

      {modo === 'crear' && (
        <>
          <FormField label="Correo electrónico" obligatorio error={errors.email?.message} ayuda="Será el usuario para iniciar sesión.">
            <input type="email" autoComplete="off" spellCheck={false} maxLength={320} {...register('email')} />
          </FormField>
          {/* autoComplete="new-password": el navegador no debe rellenar la contraseña del administrador. */}
          <FormField label="Contraseña inicial" obligatorio error={errors.password?.message} ayuda={REQUISITOS_PASSWORD}>
            <input type="password" autoComplete="new-password" maxLength={128} {...register('password')} />
          </FormField>
          <FormField label="Repite la contraseña" obligatorio error={errors.confirmacion?.message}>
            <input type="password" autoComplete="new-password" maxLength={128} {...register('confirmacion')} />
          </FormField>
        </>
      )}

      {/* En la cuenta propia el rol y el estado se muestran como texto y no se registran: RHF conserva los
          valores iniciales (un control disabled se enviaria vacio). La API igual rechaza el cambio. */}
      {esCuentaPropia ? (
        <div className="campo">
          <p className="campo__etiqueta">Rol</p>
          <p>{roles.data?.find((r) => String(r.id) === valoresIniciales?.rolId)?.nombre ?? 'Cargando…'}</p>
          <p className="campo__ayuda">No puedes cambiar el rol ni desactivar tu propia cuenta.</p>
        </div>
      ) : (
        <FormField label="Rol" obligatorio error={errors.rolId?.message} ayuda="Define qué puede ver y hacer en SentryGuard.">
          <select key={roles.isPending ? 'cargando' : 'listo'} disabled={roles.isPending} {...register('rolId')}>
            <option value="">{roles.isPending ? 'Cargando roles…' : 'Selecciona un rol'}</option>
            {roles.data?.map((r) => <option key={r.id} value={String(r.id)}>{r.nombre}</option>)}
          </select>
        </FormField>
      )}

      {esCliente && (
        <FormField label="Organización" obligatorio error={errors.organizacionId?.message} ayuda="Un Cliente solo verá los activos e incidentes de esta organización.">
          <select key={organizaciones.isPending ? 'cargando' : 'listo'} disabled={organizaciones.isPending} {...register('organizacionId')}>
            <option value="">{organizaciones.isPending ? 'Cargando organizaciones…' : 'Selecciona una organización'}</option>
            {organizaciones.data?.map((o) => <option key={o.id} value={String(o.id)}>{o.razonSocial}</option>)}
          </select>
        </FormField>
      )}

      {modo === 'editar' && !esCuentaPropia && (
        <div className="campo">
          <label className="casilla">
            <input type="checkbox" aria-describedby="ayuda-activo" {...register('activo')} />
            Cuenta activa
          </label>
          <p id="ayuda-activo" className="campo__ayuda">
            Una cuenta desactivada no puede iniciar sesión. Se conserva para no perder la bitácora ni las evidencias que registró.
          </p>
          {errors.activo?.message && <p className="campo__error"><span aria-hidden="true">⚠ </span>{errors.activo.message}</p>}
        </div>
      )}

      {(roles.isError || organizaciones.isError) && (
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
