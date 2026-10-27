import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { z } from 'zod'
import { isApiError } from '../../api/apiError'
import { mensajeDeError } from '../../components/ErrorState'
import { FormField } from '../../components/FormField'
import { Icono } from '../../components/Icono'
import { Logo } from '../../components/Layout'
import { PageHeading } from '../../components/PageHeading'
import { useAuth } from './useAuth'

const loginSchema = z.object({
  email: z.string().trim().min(1, 'Ingresa tu correo.').pipe(z.email('Ingresa un correo válido.')),
  password: z.string().min(1, 'Ingresa tu contraseña.'),
})
type LoginForm = z.infer<typeof loginSchema>

// Solo se vuelve a rutas internas de la app: evita redirecciones abiertas a sitios externos.
function destinoSeguro(desde: unknown): string {
  return typeof desde === 'string' && desde.startsWith('/') && !desde.startsWith('//') && desde !== '/login'
    ? desde
    : '/incidentes'
}

export function LoginPage() {
  const { estado, iniciarSesion, motivoCierre } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const destino = destinoSeguro((location.state as { desde?: unknown } | null)?.desde)
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '' } })

  if (estado === 'autenticado') return <Navigate to={destino} replace />

  const enviar = handleSubmit(async ({ email, password }) => {
    setErrorGeneral(null)
    try {
      await iniciarSesion(email, password)
      navigate(destino, { replace: true })
    } catch (error) {
      // 401 (credenciales) y 429 (limite de intentos) traen un mensaje pensado para el usuario.
      setErrorGeneral(isApiError(error) ? error.message : mensajeDeError(error))
    }
  })

  return (
    <div className="login">
      {/* Panel de marca: decorativo y solo en pantallas anchas; el contenido esencial esta en <main>. */}
      <aside className="login__marca-panel" aria-label="Acerca de SentryGuard">
        <p className="marca marca--grande">
          <Logo />
          <span>SentryGuard</span>
        </p>
        <p className="login__lema">Gestión de incidentes de ciberseguridad para CyberOps Chile SpA.</p>
        <ul className="login__puntos">
          <li><Icono nombre="incidentes" />Registro y seguimiento de incidentes con bitácora y evidencias</li>
          <li><Icono nombre="reloj" />Cumplimiento del SLA según el contrato de cada organización</li>
          <li><Icono nombre="candado" />Acceso por rol: cada usuario ve solo lo que le corresponde</li>
        </ul>
      </aside>

      <main id="contenido" className="login__principal">
      <div className="login__tarjeta">
        <p className="login__marca" aria-hidden="true"><Logo />SentryGuard</p>
        <PageHeading tituloDocumento="Iniciar sesión">Iniciar sesión</PageHeading>
        <p className="introduccion">Ingresa con tu cuenta de la plataforma de gestión de incidentes.</p>

        <div role="alert" className="resumen-errores-contenedor">
          {errorGeneral && <div className="resumen-errores"><p>{errorGeneral}</p></div>}
        </div>
        <div role="status" className="aviso-contenedor">
          {!errorGeneral && motivoCierre === 'expirada' && (
            <p className="aviso">Tu sesión expiró. Vuelve a iniciar sesión para continuar.</p>
          )}
          {!errorGeneral && motivoCierre === 'manual' && <p className="aviso">Cerraste sesión correctamente.</p>}
        </div>

        <form className="formulario" onSubmit={enviar} noValidate>
          <FormField label="Correo electrónico" obligatorio error={errors.email?.message}>
            <input type="email" autoComplete="username" inputMode="email" {...register('email')} />
          </FormField>
          <FormField label="Contraseña" obligatorio error={errors.password?.message}>
            <input type="password" autoComplete="current-password" {...register('password')} />
          </FormField>
          <button type="submit" className="boton boton--primario boton--bloque" disabled={isSubmitting}>
            {isSubmitting ? 'Ingresando…' : 'Ingresar'}
          </button>
        </form>
      </div>
      </main>
    </div>
  )
}
