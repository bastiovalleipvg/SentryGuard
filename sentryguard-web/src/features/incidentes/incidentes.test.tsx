import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { API, crearIncidente } from '../../test/fixtures'
import { renderApp } from '../../test/renderApp'
import { server } from '../../test/server'

describe('CRUD de incidentes', () => {
  it('lista los incidentes devueltos por la API', async () => {
    renderApp('/incidentes', { sesion: 'cliente' })

    const lista = await screen.findByRole('list', { name: 'Listado de incidentes' })
    expect(within(lista).getAllByRole('article')).toHaveLength(2)
    expect(within(lista).getByRole('link', { name: 'Ransomware en servidor de archivos' })).toHaveAttribute('href', '/incidentes/1')
    expect(screen.getByText('2 incidentes encontrados.')).toBeInTheDocument()
  })

  it('muestra el estado vacío cuando no hay resultados', async () => {
    server.use(http.get(`${API}/api/v1/incidentes`, () => HttpResponse.json({ total: 0, page: 1, pageSize: 10, totalPages: 0, items: [] })))
    renderApp('/incidentes', { sesion: 'cliente' })

    expect(await screen.findByRole('heading', { name: 'Todavía no hay incidentes' })).toBeInTheDocument()
  })

  it('muestra un error y permite reintentar si la API falla', async () => {
    server.use(http.get(`${API}/api/v1/incidentes`, () => new HttpResponse(null, { status: 500 })))
    renderApp('/incidentes', { sesion: 'cliente' })

    const mensaje = await screen.findByText(/ocurrió un error en el servidor/i)
    const alerta = mensaje.closest<HTMLElement>('[role="alert"]')!
    expect(alerta).not.toBeNull()
    expect(within(alerta).getByRole('button', { name: 'Reintentar' })).toBeInTheDocument()
  })

  it('rechaza una respuesta que no cumple el contrato de la API', async () => {
    server.use(http.get(`${API}/api/v1/incidentes`, () => HttpResponse.json({ items: [{ id: 'no-es-numero' }] })))
    renderApp('/incidentes', { sesion: 'cliente' })

    expect(await screen.findByText(/la respuesta del servidor no tiene el formato esperado/i)).toBeInTheDocument()
  })

  it('crea un incidente con datos válidos y envía el contrato esperado', async () => {
    let cuerpoEnviado: unknown = null
    server.use(
      http.post(`${API}/api/v1/incidentes`, async ({ request }) => {
        cuerpoEnviado = await request.json()
        return HttpResponse.json({ id: 1, titulo: 'x' }, { status: 201 })
      }),
    )
    const { user, router } = renderApp('/incidentes/nuevo', { sesion: 'cliente' })

    await user.type(await screen.findByLabelText(/título/i), 'Acceso no autorizado al portal')
    await user.type(screen.getByLabelText(/descripción/i), 'Inicios de sesión desde una IP desconocida.')
    await user.selectOptions(await screen.findByLabelText(/activo afectado/i), await screen.findByRole('option', { name: /firewall perimetral/i }))
    await user.selectOptions(screen.getByLabelText(/severidad/i), await screen.findByRole('option', { name: /alta/i }))
    await user.click(screen.getByRole('button', { name: 'Registrar incidente' }))

    expect(await screen.findByText('Incidente registrado correctamente.')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/incidentes/1')
    expect(cuerpoEnviado).toEqual({
      titulo: 'Acceso no autorizado al portal',
      descripcion: 'Inicios de sesión desde una IP desconocida.',
      activoId: 2,
      severidadId: 3,
    })
  })

  it('valida el formulario en el cliente antes de enviar', async () => {
    const { user } = renderApp('/incidentes/nuevo', { sesion: 'cliente' })

    await user.type(await screen.findByLabelText(/título/i), 'abc')
    await user.click(screen.getByRole('button', { name: 'Registrar incidente' }))

    expect(await screen.findByText('El título debe tener al menos 5 caracteres.')).toBeInTheDocument()
    expect(screen.getByText('Selecciona el activo afectado.')).toBeInTheDocument()
    expect(screen.getByLabelText(/título/i)).toHaveFocus()
  })

  it('muestra junto al campo los errores de validación devueltos por la API (400)', async () => {
    const { user } = renderApp('/incidentes/nuevo', { sesion: 'cliente' })

    await user.type(await screen.findByLabelText(/título/i), 'Incidente duplicado de prueba')
    await user.type(screen.getByLabelText(/descripción/i), 'Descripción con el largo mínimo exigido.')
    await user.selectOptions(await screen.findByLabelText(/activo afectado/i), await screen.findByRole('option', { name: /servidor web/i }))
    await user.selectOptions(screen.getByLabelText(/severidad/i), await screen.findByRole('option', { name: /baja/i }))
    await user.click(screen.getByRole('button', { name: 'Registrar incidente' }))

    expect(await screen.findByText('Ya existe un incidente con ese título.')).toBeInTheDocument()
    expect(screen.getByLabelText(/título/i)).toHaveAccessibleDescription(/ya existe un incidente/i)
    expect(screen.getByText(/el servidor rechazó algunos datos/i).closest('[role="alert"]')).not.toBeNull()
  })

  it('elimina un incidente solo después de confirmar en el diálogo (Administrador)', async () => {
    let eliminado = false
    server.use(
      http.delete(`${API}/api/v1/incidentes/:id`, () => {
        eliminado = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { user, router } = renderApp('/incidentes/1', { sesion: 'admin' })

    await user.click(await screen.findByRole('button', { name: 'Eliminar' }))
    const dialogo = await screen.findByRole('dialog', { name: '¿Eliminar este incidente?' })
    expect(eliminado).toBe(false)
    expect(within(dialogo).getByRole('button', { name: 'Cancelar' })).toHaveFocus()

    await user.click(within(dialogo).getByRole('button', { name: 'Sí, eliminar' }))

    expect(await screen.findByText('Incidente “Ransomware en servidor de archivos” eliminado.')).toBeInTheDocument()
    expect(eliminado).toBe(true)
    expect(router.state.location.pathname).toBe('/incidentes')
  })

  it('renderiza como texto el contenido malicioso devuelto por la API (XSS)', async () => {
    const payload = '<img src=x onerror="window.xssEjecutado=1"><script>window.xssEjecutado=1</script>'
    server.use(http.get(`${API}/api/v1/incidentes/:id`, () => HttpResponse.json(crearIncidente({ titulo: payload, descripcion: payload }))))
    const { container } = renderApp('/incidentes/1', { sesion: 'cliente' })

    expect(await screen.findByRole('heading', { level: 1, name: payload })).toBeInTheDocument()
    expect(container.querySelector('img, script')).toBeNull()
    expect((window as unknown as { xssEjecutado?: number }).xssEjecutado).toBeUndefined()
  })

  it('sugiere clasificación con IA y permite aplicar la severidad sugerida (RF-23)', async () => {
    const { user } = renderApp('/incidentes/nuevo', { sesion: 'cliente' })

    const botonIa = await screen.findByRole('button', { name: /sugerir clasificación con ia/i })
    expect(botonIa).toBeDisabled()

    await user.type(screen.getByLabelText(/título/i), 'Ataque de Ransomware detectado')
    await user.type(screen.getByLabelText(/descripción/i), 'Archivos cifrados con extensión .locked en storage central.')

    expect(botonIa).toBeEnabled()
    await user.click(botonIa)

    expect(await screen.findByText('Ransomware / Malware')).toBeInTheDocument()
    expect(screen.getByText('Severidad sugerida:')).toBeInTheDocument()

    const botonAplicar = screen.getByRole('button', { name: /aplicar sugerencia a severidad/i })
    await user.click(botonAplicar)

    const selectSeveridad = screen.getByLabelText(/severidad/i) as HTMLSelectElement
    expect(selectSeveridad.value).toBe('4')
  })

  it('maneja de forma resiliente la caída del servicio de IA sin bloquear el formulario (RNF-10)', async () => {
    server.use(
      http.post(`${API}/api/v1/ia/clasificar-incidente`, () => new HttpResponse(null, { status: 504 }))
    )
    const { user } = renderApp('/incidentes/nuevo', { sesion: 'cliente' })

    await user.type(await screen.findByLabelText(/título/i), 'Falla de red masiva')
    await user.type(screen.getByLabelText(/descripción/i), 'Pérdida de conectividad en enlaces redundantes.')

    const botonIa = screen.getByRole('button', { name: /sugerir clasificación con ia/i })
    await user.click(botonIa)

    expect(await screen.findByText(/el servicio de ia no respondió a tiempo/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Registrar incidente' })).toBeEnabled()
  })
})
