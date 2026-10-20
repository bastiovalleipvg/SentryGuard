import { http, HttpResponse } from 'msw'
import { API, INCIDENTES, USUARIOS } from './fixtures'

function usuarioDeLaPeticion(request: Request) {
  const token = request.headers.get('Authorization')?.replace('Bearer ', '')
  return Object.values(USUARIOS).find((u) => u.token === token) ?? null
}

const noAutorizado = () => new HttpResponse(null, { status: 401 })

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

  http.get(`${API}/api/v1/activos`, ({ request }) => {
    if (!usuarioDeLaPeticion(request)) return noAutorizado()
    return HttpResponse.json({
      total: 2,
      page: 1,
      pageSize: 50,
      items: [
        { id: 1, nombre: 'Servidor web de producción', hostnameIp: '10.10.0.10', categoria: 'Servidor' },
        { id: 2, nombre: 'Firewall perimetral', hostnameIp: '10.10.0.1', categoria: 'Red' },
      ],
    })
  }),
]
