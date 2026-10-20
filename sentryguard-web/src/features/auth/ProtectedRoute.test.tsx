import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { API } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { server } from '../../test/server'

describe('Rutas protegidas', () => {
  it('redirige al login cuando no hay sesión', async () => {
    const { router } = renderApp('/incidentes')

    expect(await screen.findByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
  })

  it('vuelve a la ruta pedida después de iniciar sesión', async () => {
    const { user, router } = renderApp('/incidentes/2')

    await user.type(await screen.findByLabelText(/correo electrónico/i), 'cliente@sentryguard.cl')
    await user.type(screen.getByLabelText(/contraseña/i), 'Admin123!')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Phishing dirigido a contabilidad' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/incidentes/2')
  })

  it('muestra acceso denegado cuando el rol no puede entrar a la ruta', async () => {
    // Un Cliente no puede editar incidentes (solo Administrador, Supervisor o Analista asignado).
    const { router } = renderApp('/incidentes/1/editar', { sesion: 'cliente' })

    expect(await screen.findByRole('heading', { level: 1, name: 'Acceso denegado' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/acceso-denegado')
  })

  it('muestra acceso denegado cuando la API responde 403', async () => {
    server.use(http.get(`${API}/api/v1/incidentes/:id`, () => new HttpResponse(null, { status: 403 })))
    renderApp('/incidentes/1', { sesion: 'cliente' })

    expect(await screen.findByRole('heading', { level: 1, name: 'Acceso denegado' })).toBeInTheDocument()
  })

  it('cierra la sesión y vuelve al login cuando el token expiró (401)', async () => {
    server.use(http.get(`${API}/api/v1/incidentes`, () => new HttpResponse(null, { status: 401 })))
    const { router } = renderApp('/incidentes', { sesion: 'cliente' })

    expect(await screen.findByText('Tu sesión expiró. Vuelve a iniciar sesión para continuar.')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
    expect(sessionStorage.getItem('sentryguard.sesion')).toBeNull()
  })

  it('oculta las acciones que el rol no puede realizar', async () => {
    renderApp('/incidentes/1', { sesion: 'cliente' })

    await screen.findByRole('heading', { level: 1, name: 'Ransomware en servidor de archivos' })
    expect(screen.queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Editar' })).not.toBeInTheDocument()
  })
})
