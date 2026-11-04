import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { API, crearIncidente } from '../test/fixtures'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

// Recursos relacionados del caso: activos, organizaciones, usuarios, catalogos, bitacora y evidencias.

describe('Menú por rol', () => {
  it('el Cliente ve "Mi organización" y no las vistas de administración', async () => {
    renderApp('/incidentes', { sesion: 'cliente' })
    const menu = await screen.findByRole('navigation', { name: 'Principal' })

    expect(within(menu).getByRole('link', { name: 'Mi organización', hidden: true })).toBeInTheDocument()
    for (const nombre of ['Organizaciones', 'Usuarios', 'Catálogos', 'Auditoría']) {
      expect(within(menu).queryByRole('link', { name: nombre, hidden: true })).not.toBeInTheDocument()
    }
  })

  it('el menú móvil se despliega con un botón que informa su estado', async () => {
    const { user } = renderApp('/incidentes', { sesion: 'admin' })
    const boton = await screen.findByRole('button', { name: /menú/i })

    expect(boton).toHaveAttribute('aria-expanded', 'false')
    await user.click(boton)
    expect(boton).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByRole('link', { name: 'Auditoría' })).toBeVisible()
  })

  it('un Supervisor que abre /usuarios por URL ve acceso denegado', async () => {
    renderApp('/usuarios', { sesion: 'supervisor' })
    expect(await screen.findByRole('heading', { level: 1, name: /acceso denegado/i })).toBeInTheDocument()
  })
})

describe('Activos', () => {
  it('el Supervisor registra un activo: valida en el cliente y envía el contrato de la API', async () => {
    let cuerpo: unknown = null
    server.use(
      http.post(`${API}/api/v1/activos`, async ({ request }) => {
        cuerpo = await request.json()
        return HttpResponse.json(
          { id: 10, nombre: 'Base de datos', hostnameIp: 'db01.demo.cl', categoriaId: 1, categoria: 'Servidor', organizacionId: 2, organizacion: 'Otra Empresa Ltda' },
          { status: 201 },
        )
      }),
    )
    const { user, router } = renderApp('/activos/nuevo', { sesion: 'supervisor' })

    await user.type(await screen.findByLabelText(/^nombre/i), 'Base de datos')
    await user.type(screen.getByLabelText(/hostname o ip/i), '<script>')
    await user.selectOptions(screen.getByLabelText(/categoría/i), await screen.findByRole('option', { name: 'Servidor' }))
    await user.selectOptions(screen.getByLabelText(/organización/i), await screen.findByRole('option', { name: /otra empresa/i }))
    await user.click(screen.getByRole('button', { name: 'Registrar activo' }))

    // Una entrada con caracteres no validos no llega a la API.
    expect(await screen.findByText(/usa un nombre de host o una ip/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/hostname o ip/i)).toHaveAttribute('aria-invalid', 'true')
    expect(cuerpo).toBeNull()

    await user.clear(screen.getByLabelText(/hostname o ip/i))
    await user.type(screen.getByLabelText(/hostname o ip/i), 'db01.demo.cl')
    await user.click(screen.getByRole('button', { name: 'Registrar activo' }))

    expect(await screen.findByText('Activo registrado correctamente.')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/activos/10')
    expect(cuerpo).toEqual({ nombre: 'Base de datos', hostnameIp: 'db01.demo.cl', categoriaId: 1, organizacionId: 2 })
  })

  it('el Supervisor no ve "Eliminar" y el Cliente no ve "Editar" en el detalle', async () => {
    renderApp('/activos/1', { sesion: 'supervisor' })
    expect(await screen.findByRole('heading', { level: 1, name: 'Servidor web de producción' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Editar' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument()
  })

  it('si la API rechaza eliminar un activo con incidentes (409), muestra el motivo', async () => {
    const { user } = renderApp('/activos/1', { sesion: 'admin' })

    await user.click(await screen.findByRole('button', { name: 'Eliminar' }))
    await user.click(screen.getByRole('button', { name: 'Sí, eliminar' }))

    expect(await screen.findByText('No se puede eliminar el activo porque tiene incidentes registrados.')).toBeInTheDocument()
  })
})

describe('Usuarios', () => {
  it('exige una contraseña robusta y muestra junto al campo el correo duplicado que rechaza la API', async () => {
    const { user } = renderApp('/usuarios/nuevo', { sesion: 'admin' })

    await user.type(await screen.findByLabelText(/nombre completo/i), 'Nuevo Cliente')
    await user.type(screen.getByLabelText(/correo electrónico/i), 'cliente@sentryguard.cl')
    await user.type(screen.getByLabelText(/contraseña inicial/i), 'debil')
    await user.type(screen.getByLabelText(/repite la contraseña/i), 'debil')
    await user.selectOptions(screen.getByLabelText(/^rol/i), await screen.findByRole('option', { name: 'Cliente' }))
    await user.click(screen.getByRole('button', { name: 'Crear usuario' }))

    expect(await screen.findByText('Debe tener entre 8 y 128 caracteres.')).toBeInTheDocument()
    // Al elegir Cliente aparece la organizacion, obligatoria para ese rol.
    expect(screen.getByText('Un usuario Cliente debe pertenecer a una organización.')).toBeInTheDocument()

    for (const campo of [/contraseña inicial/i, /repite la contraseña/i]) {
      await user.clear(screen.getByLabelText(campo))
      await user.type(screen.getByLabelText(campo), 'Segura123!')
    }
    await user.selectOptions(screen.getByLabelText(/^organización/i), await screen.findByRole('option', { name: 'Empresa Demo SpA' }))
    await user.click(screen.getByRole('button', { name: 'Crear usuario' }))

    expect(await screen.findByText('Ya existe un usuario con ese correo.')).toBeInTheDocument()
    expect(screen.getByLabelText(/correo electrónico/i)).toHaveAttribute('aria-invalid', 'true')
  })
})

describe('Bitácora y evidencias del incidente', () => {
  it('el Administrador agrega una entrada a la bitácora', async () => {
    let cuerpo: unknown = null
    server.use(
      http.post(`${API}/api/v1/incidentes/:id/bitacora`, async ({ request }) => {
        cuerpo = await request.json()
        return HttpResponse.json({ id: 2 }, { status: 201 })
      }),
    )
    const { user } = renderApp('/incidentes/1', { sesion: 'admin' })

    expect(await screen.findByText('Incidente creado')).toBeInTheDocument()
    await user.type(screen.getByLabelText(/acción realizada/i), 'Se aisló el servidor de la red')
    await user.click(screen.getByRole('button', { name: 'Agregar a la bitácora' }))

    expect(await screen.findByText('Entrada agregada a la bitácora.')).toBeInTheDocument()
    expect(cuerpo).toEqual({ accion: 'Se aisló el servidor de la red', comentario: null })
  })

  it('rechaza una ubicación de evidencia con javascript: sin enviarla a la API', async () => {
    let enviada = false
    server.use(
      http.post(`${API}/api/v1/incidentes/:id/evidencia`, () => {
        enviada = true
        return HttpResponse.json({ id: 1 }, { status: 201 })
      }),
    )
    const { user } = renderApp('/incidentes/1', { sesion: 'admin' })

    await user.type(await screen.findByLabelText(/nombre del archivo/i), 'captura.png')
    await user.type(screen.getByLabelText(/^ubicación/i), 'javascript:alert(1)')
    await user.click(screen.getByRole('button', { name: 'Registrar evidencia' }))

    expect(await screen.findByText(/usa una url que empiece con https/i)).toBeInTheDocument()
    expect(enviada).toBe(false)
  })

  it('el Analista documenta su incidente asignado, pero no lo edita ni cambia el estado', async () => {
    server.use(
      http.get(`${API}/api/v1/incidentes/:id`, () =>
        HttpResponse.json(crearIncidente({ analistaAsignadoId: 3, analistaAsignado: 'Analista Demo' })),
      ),
    )
    renderApp('/incidentes/1', { sesion: 'analista' })

    expect(await screen.findByRole('button', { name: 'Agregar a la bitácora' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Registrar evidencia' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /editar/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Cambiar estado' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Gestión' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Organizaciones', hidden: true })).not.toBeInTheDocument()
  })

  it('el Cliente ve la bitácora y el SLA, pero no los formularios de registro', async () => {
    renderApp('/incidentes/1', { sesion: 'cliente' })

    expect(await screen.findByText('Dentro del plazo')).toBeInTheDocument()
    expect(await screen.findByText('Incidente creado')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Agregar a la bitácora' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Registrar evidencia' })).not.toBeInTheDocument()
  })
})

describe('Organizaciones y catálogos', () => {
  it('valida el dígito verificador del RUT antes de enviar', async () => {
    const { user } = renderApp('/organizaciones/nueva', { sesion: 'admin' })

    await user.type(await screen.findByLabelText(/razón social/i), 'Empresa Nueva SpA')
    await user.type(screen.getByLabelText(/^rut/i), '76.123.456-7')
    await user.selectOptions(screen.getByLabelText(/plan contratado/i), await screen.findByRole('option', { name: /básico/i }))
    await user.click(screen.getByRole('button', { name: 'Registrar organización' }))

    expect(await screen.findByText('El dígito verificador del RUT no es válido.')).toBeInTheDocument()
  })

  it('el Cliente abre "Mi organización" con sus contratos SLA y no puede gestionarlos', async () => {
    renderApp('/mi-organizacion', { sesion: 'cliente' })

    expect(await screen.findByRole('heading', { level: 1, name: 'Empresa Demo SpA' })).toBeInTheDocument()
    expect(await screen.findByText('Contrato #1')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Registrar contrato' })).not.toBeInTheDocument()
  })

  it('el Administrador agrega una categoría de activo', async () => {
    let cuerpo: unknown = null
    server.use(
      http.post(`${API}/api/v1/catalogos/categorias-activo`, async ({ request }) => {
        cuerpo = await request.json()
        return HttpResponse.json({ id: 9 }, { status: 201 })
      }),
    )
    const { user } = renderApp('/catalogos', { sesion: 'admin' })

    const seccion = await screen.findByRole('region', { name: 'Categorías de activo' })
    await user.type(within(seccion).getByLabelText(/^nombre/i), 'IoT')
    await user.click(within(seccion).getByRole('button', { name: 'Agregar' }))

    expect(await screen.findByText('“IoT” se agregó al catálogo.')).toBeInTheDocument()
    expect(cuerpo).toEqual({ nombre: 'IoT' })
  })
})
