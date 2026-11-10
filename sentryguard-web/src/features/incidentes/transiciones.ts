/**
 * Matriz de transiciones permitidas según el flujo de estados (ADR-008).
 * Estados:
 * 1: Abierto
 * 2: En progreso
 * 3: Escalado
 * 4: Resuelto
 * 5: Cerrado
 */
export const TRANSICIONES_PERMITIDAS: Record<number, readonly number[]> = {
  1: [2, 3, 4, 5], // Abierto -> En progreso, Escalado, Resuelto, Cerrado
  2: [3, 4, 5],    // En progreso -> Escalado, Resuelto, Cerrado
  3: [2, 4, 5],    // Escalado -> En progreso, Resuelto, Cerrado
  4: [2, 5],       // Resuelto -> En progreso (reapertura), Cerrado
  5: [1, 2],       // Cerrado -> Abierto o En progreso (reapertura por Administrador/Supervisor)
}

export interface ItemEstado {
  id: number
  nombre: string
}

export function obtenerEstadosDestinoPermitidos(
  estadoActualId: number,
  todosLosEstados: readonly ItemEstado[],
): ItemEstado[] {
  const permitidos = TRANSICIONES_PERMITIDAS[estadoActualId] ?? []
  return todosLosEstados.filter((e) => permitidos.includes(e.id))
}

