import type { Incidente, Rol, UsuarioActual } from '../../api/contracts'

/**
 * Espejo en la interfaz de la matriz RBAC de la API. Solo decide que se muestra: la API vuelve a
 * validar el rol en cada endpoint y responde 403 aunque alguien fuerce la interfaz.
 */
export const PERMISOS = {
  crearIncidente: ['Administrador', 'Supervisor', 'Cliente'],
  editarIncidente: ['Administrador', 'Supervisor', 'Analista'],
  eliminarIncidente: ['Administrador'],
  asignarAnalista: ['Administrador', 'Supervisor'],
  cambiarEstado: ['Administrador', 'Supervisor', 'Analista'],
} as const satisfies Record<string, readonly Rol[]>

export type Accion = keyof typeof PERMISOS

export function puede(usuario: UsuarioActual | null, accion: Accion): boolean {
  return usuario !== null && (PERMISOS[accion] as readonly Rol[]).includes(usuario.rol)
}

/** El Analista solo gestiona los incidentes que tiene asignados (igual que ValidaAccesoIncidente). */
export function puedeSobreIncidente(usuario: UsuarioActual | null, accion: Accion, incidente: Incidente): boolean {
  if (!puede(usuario, accion)) return false
  if (usuario?.rol === 'Analista') return incidente.analistaAsignadoId === usuario.id
  return true
}
