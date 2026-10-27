import type { Activo, Incidente, Organizacion, Usuario, UsuarioActual } from '../api/contracts'

export const API = 'https://api.sentryguard.test'

export const USUARIOS: Record<string, UsuarioActual & { password: string; token: string }> = {
  cliente: { id: 4, nombre: 'Cliente Demo', email: 'cliente@sentryguard.cl', rol: 'Cliente', organizacionId: 1, password: 'Admin123!', token: 'token-cliente' },
  admin: { id: 1, nombre: 'Administrador SentryGuard', email: 'admin@sentryguard.cl', rol: 'Administrador', organizacionId: null, password: 'Admin123!', token: 'token-admin' },
  supervisor: { id: 2, nombre: 'Supervisor Demo', email: 'supervisor@sentryguard.cl', rol: 'Supervisor', organizacionId: null, password: 'Admin123!', token: 'token-supervisor' },
  analista: { id: 3, nombre: 'Analista Demo', email: 'analista@sentryguard.cl', rol: 'Analista', organizacionId: null, password: 'Admin123!', token: 'token-analista' },
}

export function crearIncidente(parcial: Partial<Incidente> = {}): Incidente {
  return {
    id: 1,
    titulo: 'Ransomware en servidor de archivos',
    descripcion: 'Archivos cifrados en el servidor de archivos compartido.',
    severidadId: 4,
    severidad: 'Crítica',
    estadoId: 1,
    estado: 'Abierto',
    activoId: 1,
    activo: 'Servidor web de producción',
    organizacionId: 1,
    analistaAsignadoId: null,
    analistaAsignado: null,
    fechaCreacion: '2026-10-20T12:00:00',
    fechaCierre: null,
    ...parcial,
  }
}

export const INCIDENTES: Incidente[] = [
  crearIncidente(),
  crearIncidente({ id: 2, titulo: 'Phishing dirigido a contabilidad', severidadId: 2, severidad: 'Media', estadoId: 2, estado: 'En progreso' }),
]

export const ACTIVOS: Activo[] = [
  { id: 1, nombre: 'Servidor web de producción', hostnameIp: '10.10.0.10', categoriaId: 1, categoria: 'Servidor', organizacionId: 1, organizacion: 'Empresa Demo SpA' },
  { id: 2, nombre: 'Firewall perimetral', hostnameIp: '10.10.0.1', categoriaId: 3, categoria: 'Red', organizacionId: 1, organizacion: 'Empresa Demo SpA' },
]

export const ORGANIZACIONES: Organizacion[] = [
  { id: 1, razonSocial: 'Empresa Demo SpA', rut: '76123456-7', planId: 2, plan: 'Estándar', fechaAlta: '2026-01-01T00:00:00' },
  { id: 2, razonSocial: 'Otra Empresa Ltda', rut: '88888888-8', planId: 1, plan: 'Básico', fechaAlta: '2026-03-01T00:00:00' },
]

export const USUARIOS_ADMIN: Usuario[] = [
  { id: 1, nombre: 'Administrador SentryGuard', email: 'admin@sentryguard.cl', rolId: 1, rol: 'Administrador', organizacionId: null, organizacion: null, activo: true },
  { id: 4, nombre: 'Cliente Demo', email: 'cliente@sentryguard.cl', rolId: 4, rol: 'Cliente', organizacionId: 1, organizacion: 'Empresa Demo SpA', activo: true },
]

export function pagina<T>(items: T[]) {
  return { total: items.length, page: 1, pageSize: 10, totalPages: items.length ? 1 : 0, items }
}
