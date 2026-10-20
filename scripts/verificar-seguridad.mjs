#!/usr/bin/env node
// Verificacion de controles de seguridad contra la API SentryGuard en ejecucion.
// Uso (con la API corriendo en https://localhost:7166):
//   node --use-system-ca scripts/verificar-seguridad.mjs
// Genera docs/evidencias/seguridad-api.txt. Al final dispara el limite de tasa del login: espera
// un minuto antes de volver a iniciar sesion desde la misma IP.
import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const API = process.env.API_URL ?? 'https://localhost:7166'
const ORIGEN_FRONTEND = 'https://localhost:5173'
const CLAVE = 'Admin123!'
const raiz = join(dirname(fileURLToPath(import.meta.url)), '..')
const salida = join(raiz, 'docs', 'evidencias', 'seguridad-api.txt')

const lineas = []
let fallos = 0
const log = (texto = '') => {
  lineas.push(texto)
  console.log(texto)
}

function verificar(nombre, ok, detalle) {
  if (!ok) fallos += 1
  log(`[${ok ? 'OK  ' : 'FALLA'}] ${nombre}`)
  if (detalle) log(`        ${detalle}`)
}

async function llamar(metodo, ruta, { token, cuerpo, crudo, cabeceras = {} } = {}) {
  const headers = { Accept: 'application/json', ...cabeceras }
  if (token) headers.Authorization = `Bearer ${token}`
  if (cuerpo !== undefined || crudo !== undefined) headers['Content-Type'] = 'application/json'
  const r = await fetch(`${API}${ruta}`, { method: metodo, headers, body: crudo ?? (cuerpo === undefined ? undefined : JSON.stringify(cuerpo)) })
  const texto = await r.text()
  let json = null
  try { json = texto ? JSON.parse(texto) : null } catch { /* cuerpo no JSON */ }
  return { status: r.status, headers: r.headers, texto, json }
}

async function login(email, password = CLAVE) {
  const r = await llamar('POST', '/api/v1/usuarios/login', { cuerpo: { email, password } })
  if (r.status !== 200) throw new Error(`Login de ${email} fallo con ${r.status}: ${r.texto}`)
  return r.json.token
}

const recorte = (t) => (t.length > 160 ? `${t.slice(0, 160)}…` : t)

log('Verificacion de seguridad de la API SentryGuard')
log(`Fecha: ${new Date().toISOString()}`)
log(`API:   ${API}`)
log('')

log('== Autenticacion (API2) ==')
const sinToken = await llamar('GET', '/api/v1/incidentes')
verificar('Endpoint protegido sin token -> 401', sinToken.status === 401, `status=${sinToken.status}`)

const tokenFalso = await llamar('GET', '/api/v1/incidentes', { token: 'eyJhbGciOiJIUzI1NiJ9.e30.firma-falsa' })
verificar('Token invalido o manipulado -> 401', tokenFalso.status === 401, `status=${tokenFalso.status}`)

const claveMala = await llamar('POST', '/api/v1/usuarios/login', { cuerpo: { email: 'cliente@sentryguard.cl', password: 'Incorrecta1!' } })
const emailInexistente = await llamar('POST', '/api/v1/usuarios/login', { cuerpo: { email: 'no-existe@sentryguard.cl', password: 'Incorrecta1!' } })
verificar(
  'Login fallido: mismo 401 y mismo mensaje para clave incorrecta y email inexistente (sin enumeracion)',
  claveMala.status === 401 && emailInexistente.status === 401 && claveMala.texto === emailInexistente.texto,
  `cuerpo=${recorte(claveMala.texto)}`,
)

const tokenCliente = await login('cliente@sentryguard.cl')
const tokenAdmin = await login('admin@sentryguard.cl')
log('')

log('== Autorizacion por funcion (API5) ==')
const borrarComoCliente = await llamar('DELETE', '/api/v1/incidentes/1', { token: tokenCliente })
verificar('Cliente intenta eliminar un incidente -> 403', borrarComoCliente.status === 403, `status=${borrarComoCliente.status}`)
const editarComoCliente = await llamar('PUT', '/api/v1/incidentes/1', { token: tokenCliente, cuerpo: { titulo: 'Cambio no autorizado' } })
verificar('Cliente intenta editar un incidente -> 403', editarComoCliente.status === 403, `status=${editarComoCliente.status}`)
const registroComoCliente = await llamar('POST', '/api/v1/usuarios/registro', { token: tokenCliente, cuerpo: { nombre: 'x', email: 'x@x.cl', password: 'x', rolId: 1 } })
verificar('Cliente intenta crear un usuario Administrador -> 403', registroComoCliente.status === 403, `status=${registroComoCliente.status}`)
log('')

log('== Autorizacion por objeto (API1) ==')
const rut = `${Math.floor(10_000_000 + Math.random() * 89_999_999)}-${Math.floor(Math.random() * 10)}`
const org = await llamar('POST', '/api/v1/organizaciones', { token: tokenAdmin, cuerpo: { razonSocial: `Otra organizacion ${rut}`, rut, planId: 1 } })
const activo = await llamar('POST', '/api/v1/activos', { token: tokenAdmin, cuerpo: { organizacionId: org.json?.id, nombre: 'Activo de otra organizacion', hostnameIp: '10.20.0.1', categoriaId: 1 } })
const ajeno = await llamar('POST', '/api/v1/incidentes', { token: tokenAdmin, cuerpo: { titulo: 'Incidente de otra organizacion', descripcion: 'Creado por el script de verificacion.', activoId: activo.json?.id, severidadId: 2 } })
const idAjeno = ajeno.json?.id
const verAjeno = await llamar('GET', `/api/v1/incidentes/${idAjeno}`, { token: tokenCliente })
verificar(`Cliente (org 1) pide por id un incidente de otra organizacion (#${idAjeno}) -> 403`, verAjeno.status === 403, `status=${verAjeno.status}`)
const crearEnAjeno = await llamar('POST', '/api/v1/incidentes', { token: tokenCliente, cuerpo: { titulo: 'Intento cruzado', descripcion: 'Activo de otra organizacion.', activoId: activo.json?.id, severidadId: 1 } })
verificar('Cliente crea un incidente sobre un activo de otra organizacion -> 403', crearEnAjeno.status === 403, `status=${crearEnAjeno.status}`)
const listaCliente = await llamar('GET', '/api/v1/incidentes?pageSize=50', { token: tokenCliente })
verificar('El listado del Cliente no incluye incidentes ajenos', listaCliente.status === 200 && !listaCliente.json.items.some((i) => i.id === idAjeno))
log('')

log('== Validacion de entradas e inyeccion ==')
const invalido = await llamar('POST', '/api/v1/incidentes', { token: tokenCliente, cuerpo: { titulo: '', descripcion: 'corta', activoId: 1, severidadId: 99 } })
verificar('Datos invalidos -> 400 con errores por campo', invalido.status === 400 && invalido.json?.errors?.titulo && invalido.json?.errors?.severidadId, `errores=${JSON.stringify(invalido.json?.errors)}`)
const malformado = await llamar('POST', '/api/v1/incidentes', { token: tokenCliente, crudo: '{"titulo": ' })
verificar(
  'JSON mal formado -> 400 sin detalles internos (sin stack trace ni nombres de excepcion)',
  malformado.status === 400 && !/Exception|System\.|\s+at\s/.test(malformado.texto),
  `cuerpo=${recorte(malformado.texto || '(vacio)')}`,
)
const busquedaSql = await llamar('GET', `/api/v1/incidentes?q=${encodeURIComponent("' OR 1=1; DROP TABLE tb_Incidente;--")}`, { token: tokenCliente })
verificar('Busqueda con inyeccion SQL -> 200 sin resultados (EF Core parametriza la consulta)', busquedaSql.status === 200 && busquedaSql.json.total === 0, `total=${busquedaSql.json?.total}`)
const payload = '<img src=x onerror=alert(1)><script>alert(1)</script>'
const conScript = await llamar('POST', '/api/v1/incidentes', { token: tokenCliente, cuerpo: { titulo: `XSS ${payload}`, descripcion: payload, activoId: 1, severidadId: 1 } })
const leido = await llamar('GET', `/api/v1/incidentes/${conScript.json?.id}`, { token: tokenCliente })
verificar(
  'Payload XSS se guarda y se devuelve como dato JSON (el frontend lo renderiza como texto)',
  leido.status === 200 && leido.headers.get('content-type')?.startsWith('application/json') && leido.headers.get('x-content-type-options') === 'nosniff',
  `content-type=${leido.headers.get('content-type')}; csp=${leido.headers.get('content-security-policy')}`,
)
log('')

log('== Exposicion de datos (API3) ==')
const detalle = await llamar('GET', '/api/v1/incidentes/1', { token: tokenAdmin })
verificar('Las respuestas no incluyen password_hash ni datos internos', !/password|hash/i.test(detalle.texto), `campos=${Object.keys(detalle.json ?? {}).join(', ')}`)
const paginaGrande = await llamar('GET', '/api/v1/incidentes?pageSize=100000', { token: tokenAdmin })
verificar('pageSize excesivo se acota a 50 (API4)', paginaGrande.json?.pageSize === 50, `pageSize=${paginaGrande.json?.pageSize}`)
log('')

log('== Configuracion para el navegador (API8) ==')
const preflight = async (origen) =>
  llamar('OPTIONS', '/api/v1/usuarios/login', { cabeceras: { Origin: origen, 'Access-Control-Request-Method': 'POST', 'Access-Control-Request-Headers': 'content-type' } })
const permitido = await preflight(ORIGEN_FRONTEND)
verificar(`CORS: preflight desde ${ORIGEN_FRONTEND} recibe Access-Control-Allow-Origin`, permitido.headers.get('access-control-allow-origin') === ORIGEN_FRONTEND)
const malicioso = await preflight('https://sitio-malicioso.example')
verificar('CORS: preflight desde https://sitio-malicioso.example NO recibe Access-Control-Allow-Origin', !malicioso.headers.get('access-control-allow-origin'))
const h = (await llamar('GET', '/api/v1/catalogos/severidades')).headers
verificar(
  'Cabeceras de seguridad presentes',
  h.get('x-content-type-options') === 'nosniff' && h.get('x-frame-options') === 'DENY' && h.get('content-security-policy')?.includes("default-src 'none'") && h.get('cache-control') === 'no-store',
  ['x-content-type-options', 'x-frame-options', 'referrer-policy', 'content-security-policy', 'cache-control'].map((n) => `${n}: ${h.get(n)}`).join(' | '),
)
log('')

log('== Limite de tasa en el login (API4) ==')
let respuesta429 = null
const estados = []
for (let i = 1; i <= 7 && !respuesta429; i += 1) {
  const r = await llamar('POST', '/api/v1/usuarios/login', { cuerpo: { email: 'cliente@sentryguard.cl', password: 'Incorrecta1!' } })
  estados.push(r.status)
  if (r.status === 429) respuesta429 = r
}
verificar(
  'Intentos repetidos de login terminan en 429 con Retry-After y mensaje claro',
  respuesta429 !== null && Number(respuesta429.headers.get('retry-after')) > 0,
  `estados=${estados.join(',')}; retry-after=${respuesta429?.headers.get('retry-after')}; cuerpo=${respuesta429 ? recorte(respuesta429.texto) : '-'}`,
)
log('')
log(fallos === 0 ? 'Resultado: todos los controles verificados.' : `Resultado: ${fallos} control(es) con falla.`)

mkdirSync(dirname(salida), { recursive: true })
writeFileSync(salida, `${lineas.join('\n')}\n`, 'utf8')
console.log(`\nEvidencia guardada en ${salida}`)
process.exitCode = fallos === 0 ? 0 : 1
