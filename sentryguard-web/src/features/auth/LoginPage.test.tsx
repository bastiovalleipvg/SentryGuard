import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderApp } from '../../test/renderApp'

describe('Inicio de sesión', () => {
  it('inicia sesión, guarda el token y redirige al listado de incidentes', async () => {
    const { user } = renderApp('/login')

    await user.type(screen.getByLabelText(/correo electrónico/i), 'cliente@sentryguard.cl')
    await user.type(screen.getByLabelText(/contraseña/i), 'Admin123!')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Incidentes' })).toBeInTheDocument()
    expect(screen.getByText('Rol: Cliente')).toBeInTheDocument()
    expect(sessionStorage.getItem('sentryguard.sesion')).toContain('token-cliente')
  })

  it('valida los campos en el cliente y asocia los errores a cada campo', async () => {
    const { user } = renderApp('/login')

    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByText('Ingresa tu correo.')).toBeInTheDocument()
    expect(screen.getByText('Ingresa tu contraseña.')).toBeInTheDocument()
    expect(screen.getByLabelText(/correo electrónico/i)).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText(/correo electrónico/i)).toHaveAccessibleDescription(/ingresa tu correo/i)
  })

  it('muestra el mensaje genérico de la API ante credenciales inválidas', async () => {
    const { user } = renderApp('/login')

    await user.type(screen.getByLabelText(/correo electrónico/i), 'cliente@sentryguard.cl')
    await user.type(screen.getByLabelText(/contraseña/i), 'incorrecta')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByText(/correo o contraseña incorrectos/i)).toBeInTheDocument()
    expect(screen.getByText(/correo o contraseña incorrectos/i).closest('[role="alert"]')).not.toBeNull()
    expect(sessionStorage.getItem('sentryguard.sesion')).toBeNull()
  })

  it('informa con claridad cuando la API limita los intentos (429)', async () => {
    const { user } = renderApp('/login')

    await user.type(screen.getByLabelText(/correo electrónico/i), 'bloqueado@sentryguard.cl')
    await user.type(screen.getByLabelText(/contraseña/i), 'cualquiera')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByText(/demasiados intentos\. espera 60 segundos/i)).toBeInTheDocument()
    expect(screen.getByText(/demasiados intentos/i).closest('[role="alert"]')).not.toBeNull()
  })
})
