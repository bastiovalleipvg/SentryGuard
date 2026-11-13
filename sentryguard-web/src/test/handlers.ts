import { http, HttpResponse } from 'msw'
import { ACTIVOS, API, INCIDENTES, ORGANIZACIONES, USUARIOS, USUARIOS_ADMIN, pagina } from './fixtures'

function usuarioDeLaPeticion(request: Request) {
  const token = request.headers.get('Authorization')?.replace('Bearer ', '')
  return Object.values(USUARIOS).find((u) => u.token === token) ?? null
}

const noAutorizado = () => new HttpResponse(null, { status: 401 })
const prohibido = () => new HttpResponse(null, { status: 403 })

/** Simula RequireRole de la API: 401 sin token, 403 si el rol no esta en la lista. */
function exigirRol(request: Request, roles: string[]) {
  const usuario = usuarioDeLaPeticion(request)
  if (!usuario) return noAutorizado()
  if (!roles.includes(usuario.rol)) return prohibido()
  return null
}

// API simulada con el mismo contrato que SentryGuard (incluidos 401, 403, 400 y 429).
export const handlers = [
  http.post(`${API}/api/v1/usuarios/login`, async ({ request }) => {
    const { email, password } = (await request.json()) as { email: string; password: string }
    if (email === 'bloqueado@sentryguard.cl') {
      return HttpResponse.json(
        { codigo: 'DEMASIADAS_SOLICITUDES', mensaje: 'Demasiados intentos. Espera 60 segundos antes de volver a intentarlo.' },
        { status: 429, headers: { 'Retry-After': '60' } },
      )
    }
    const usuario = Object.values(USUARIOS).find((u) => u.email === email && u.password === password)
    if (!usuario) {
      return HttpResponse.json(
        { codigo: 'CREDENCIALES_INVALIDAS', mensaje: 'Correo o contraseña incorrectos, o la cuenta está bloqueada temporalmente.' },
        { status: 401 },
      )
    }
    return HttpResponse.json({ token: usuario.token, refreshToken: 'r', expiresIn: 3600 })
  }),

  http.get(`${API}/api/v1/usuarios/me`, ({ request }) => {
    const usuario = usuarioDeLaPeticion(request)
    if (!usuario) return noAutorizado()
    const { password: _p, token: _t, ...publico } = usuario
    return HttpResponse.json({ ...publico, activo: true })
  }),

  http.get(`${API}/api/v1/usuarios/analistas`, () => HttpResponse.json([{ id: 3, nombre: 'Analista Demo' }])),

  http.get(`${API}/api/v1/incidentes`, ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    const q = new URL(request.url).searchParams.get('q')?.toLowerCase()
    const items = q ? INCIDENTES.filter((i) => i.titulo.toLowerCase().includes(q)) : INCIDENTES
    return HttpResponse.json({ total: items.length, page: 1, pageSize: 10, totalPages: 1, items })
  }),

  http.get(`${API}/api/v1/incidentes/:id`, ({ request, params }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    const incidente = INCIDENTES.find((i) => i.id === Number(params.id))
    return incidente ? HttpResponse.json(incidente) : new HttpResponse(null, { status: 404 })
  }),

  http.post(`${API}/api/v1/incidentes`, async ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    const cuerpo = (await request.json()) as { titulo: string }
    if (cuerpo.titulo.includes('duplicado')) {
      return HttpResponse.json(
        { title: 'One or more validation errors occurred.', status: 400, errors: { titulo: ['Ya existe un incidente con ese título.'] } },
        { status: 400 },
      )
    }
    return HttpResponse.json({ id: 1, titulo: cuerpo.titulo }, { status: 201 })
  }),

  http.post(`${API}/api/v1/ia/clasificar-incidente`, async ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    const { titulo } = (await request.json()) as { titulo: string }
    const esCritico = (titulo || '').toLowerCase().includes('ransomware')
    return HttpResponse.json({
      severidadSugeridaId: esCritico ? 4 : 2,
      severidadNombre: esCritico ? 'Crítica' : 'Media',
      categoriaSugerida: esCritico ? 'Ransomware / Malware' : 'Incidente Operativo',
      justificacion: 'Clasificación sugerida basada en los indicadores del título y descripción.',
      accionesRecomendadas: [
        'Aislar los equipos afectados de la red corporativa.',
        'Preservar logs y memoria para análisis forense.',
      ],
    })
  }),

  http.delete(`${API}/api/v1/incidentes/:id`, ({ request }) => {
    const usuario = usuarioDeLaPeticion(request)
    if (!usuario) return noAutorizado()
    return usuario.rol === 'Administrador' ? new HttpResponse(null, { status: 204 }) : new HttpResponse(null, { status: 403 })
  }),

  http.get(`${API}/api/v1/catalogos/severidades`, () =>
    HttpResponse.json({
      items: [
        { id: 1, nombre: 'Baja', horasSLA: 24 },
        { id: 2, nombre: 'Media', horasSLA: 12 },
        { id: 3, nombre: 'Alta', horasSLA: 4 },
        { id: 4, nombre: 'Crítica', horasSLA: 1 },
      ],
    }),
  ),

  http.get(`${API}/api/v1/catalogos/estados-incidente`, () =>
    HttpResponse.json({ items: [{ id: 1, nombre: 'Abierto' }, { id: 2, nombre: 'En progreso' }, { id: 4, nombre: 'Resuelto' }] }),
  ),

  http.get(`${API}/api/v1/catalogos/categorias-incidente`, () =>
    HttpResponse.json({ items: [{ id: 1, nombre: 'Phishing' }, { id: 2, nombre: 'Malware' }, { id: 6, nombre: 'Otro' }] }),
  ),

  http.get(`${API}/api/v1/catalogos/transiciones-estado`, () =>
    HttpResponse.json([
      { estadoOrigenId: 1, estadoDestinoId: 2 },
      { estadoOrigenId: 1, estadoDestinoId: 4 },
      { estadoOrigenId: 2, estadoDestinoId: 4 },
      { estadoOrigenId: 4, estadoDestinoId: 2 },
    ]),
  ),

  http.get(`${API}/api/v1/activos`, ({ request }) => {
    const usuario = usuarioDeLaPeticion(request)
    if (!usuario) return noAutorizado()
    const q = new URL(request.url).searchParams.get('q')?.toLowerCase()
    const items = ACTIVOS.filter((a) => (usuario.rol !== 'Cliente' || a.organizacionId === usuario.organizacionId) && (!q || a.nombre.toLowerCase().includes(q)))
    return HttpResponse.json(pagina(items))
  }),

  http.get(`${API}/api/v1/activos/:id`, ({ request, params }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    const activo = ACTIVOS.find((a) => a.id === Number(params.id))
    return activo ? HttpResponse.json(activo) : new HttpResponse(null, { status: 404 })
  }),

  http.post(`${API}/api/v1/activos`, async ({ request }) => {
    const denegado = exigirRol(request, ['Administrador', 'Supervisor'])
    if (denegado) return denegado
    const cuerpo = (await request.json()) as { nombre: string; hostnameIp: string | null; categoriaId: number; organizacionId: number }
    return HttpResponse.json({ ...ACTIVOS[0], ...cuerpo, id: 10 }, { status: 201 })
  }),

  http.delete(`${API}/api/v1/activos/:id`, ({ request }) => {
    const denegado = exigirRol(request, ['Administrador'])
    if (denegado) return denegado
    return HttpResponse.json(
      { codigo: 'ACTIVO_CON_INCIDENTES', mensaje: 'No se puede eliminar el activo porque tiene incidentes registrados.' },
      { status: 409 },
    )
  }),

  http.get(`${API}/api/v1/catalogos/categorias-activo`, () => HttpResponse.json({ items: [{ id: 1, nombre: 'Servidor' }, { id: 3, nombre: 'Red' }] })),
  http.get(`${API}/api/v1/catalogos/planes-servicio`, () =>
    HttpResponse.json({
      items: [
        { id: 1, nombre: 'Básico', descripcion: null, tiempoRespuestaBaseH: 24 },
        { id: 2, nombre: 'Estándar', descripcion: 'Plan estándar', tiempoRespuestaBaseH: 12 },
      ],
    }),
  ),
  http.get(`${API}/api/v1/catalogos/roles`, () =>
    HttpResponse.json({ items: [{ id: 1, nombre: 'Administrador' }, { id: 2, nombre: 'Supervisor' }, { id: 3, nombre: 'Analista' }, { id: 4, nombre: 'Cliente' }] }),
  ),
  http.post(`${API}/api/v1/catalogos/:tipo`, ({ request }) => {
    const denegado = exigirRol(request, ['Administrador'])
    return denegado ?? HttpResponse.json({ id: 99 }, { status: 201 })
  }),

  http.get(`${API}/api/v1/organizaciones`, ({ request }) => {
    const denegado = exigirRol(request, ['Administrador', 'Supervisor', 'Analista'])
    return denegado ?? HttpResponse.json(pagina(ORGANIZACIONES))
  }),
  http.get(`${API}/api/v1/organizaciones/:id`, ({ request, params }) => {
    const usuario = usuarioDeLaPeticion(request)
    if (!usuario) return noAutorizado()
    if (usuario.rol === 'Cliente' && usuario.organizacionId !== Number(params.id)) return prohibido()
    const org = ORGANIZACIONES.find((o) => o.id === Number(params.id))
    return org ? HttpResponse.json(org) : new HttpResponse(null, { status: 404 })
  }),
  http.get(`${API}/api/v1/organizaciones/:id/contratos-sla`, ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    return HttpResponse.json({
      total: 1,
      items: [{ id: 1, vigenteDesde: '2026-01-01T00:00:00', vigenteHasta: null, detalles: [{ severidadId: 4, severidad: 'Crítica', horasSla: 0.5 }] }],
    })
  }),

  http.get(`${API}/api/v1/usuarios`, ({ request }) => {
    const denegado = exigirRol(request, ['Administrador'])
    return denegado ?? HttpResponse.json(pagina(USUARIOS_ADMIN))
  }),
  http.post(`${API}/api/v1/usuarios/registro`, async ({ request }) => {
    const denegado = exigirRol(request, ['Administrador'])
    if (denegado) return denegado
    const cuerpo = (await request.json()) as { email: string }
    if (USUARIOS_ADMIN.some((u) => u.email === cuerpo.email)) {
      return HttpResponse.json({ status: 400, errors: { email: ['Ya existe un usuario con ese correo.'] } }, { status: 400 })
    }
    return HttpResponse.json({ id: 50 }, { status: 201 })
  }),

  http.get(`${API}/api/v1/incidentes/sla/cumplimiento/:id`, ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    return HttpResponse.json({ horasTranscurridas: 0.4, horasSLA: 1, cumple: true, fuente: 'Severidad', fechaLimite: '2026-10-20T13:00:00', cerrado: false })
  }),
  http.get(`${API}/api/v1/incidentes/:id/bitacora`, ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    return HttpResponse.json({ total: 1, items: [{ id: 1, accion: 'Incidente creado', comentario: null, usuario: 'Cliente Demo', timestamp: '2026-10-20T12:00:00' }] })
  }),
  http.post(`${API}/api/v1/incidentes/:id/bitacora`, ({ request }) => {
    const denegado = exigirRol(request, ['Administrador', 'Supervisor', 'Analista'])
    return denegado ?? HttpResponse.json({ id: 2 }, { status: 201 })
  }),
  http.get(`${API}/api/v1/incidentes/:id/evidencia`, ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    return HttpResponse.json({ total: 0, items: [] })
  }),
  http.post(`${API}/api/v1/incidentes/:id/evidencia`, ({ request }) => {
    const denegado = exigirRol(request, ['Administrador', 'Supervisor', 'Analista'])
    return denegado ?? HttpResponse.json({ id: 1 }, { status: 201 })
  }),
]
