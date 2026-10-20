import type { Incidente, UsuarioActual } from '../api/contracts'

export const API = 'https://api.sentryguard.test'

export const USUARIOS: Record<string, UsuarioActual & { password: string; token: string }> = {
  cliente: { id: 4, nombre: 'Cliente Demo', email: 'cliente@sentryguard.cl', rol: 'Cliente', organizacionId: 1, password: 'Admin123!', token: 'token-cliente' },
  admin: { id: 1, nombre: 'Administrador SentryGuard', email: 'admin@sentryguard.cl', rol: 'Administrador', organizacionId: null, password: 'Admin123!', token: 'token-admin' },
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
