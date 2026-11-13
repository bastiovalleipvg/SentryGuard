import type { Transicion } from '../../api/contracts'

/**
 * El flujo de estados (ADR-008) vive en la base de datos (tb_TransicionEstado) y la API lo expone en
 * GET /api/v1/catalogos/transiciones-estado. La interfaz solo lo usa para ofrecer destinos validos;
 * la API vuelve a validarlo y responde 409 TRANSICION_INVALIDA ante un par no permitido.
 */
export interface ItemEstado {
  id: number
  nombre: string
}

export function obtenerEstadosDestinoPermitidos(
  estadoActualId: number,
  todosLosEstados: readonly ItemEstado[],
  transiciones: readonly Transicion[],
): ItemEstado[] {
  const permitidos = new Set(transiciones.filter((t) => t.estadoOrigenId === estadoActualId).map((t) => t.estadoDestinoId))
  return todosLosEstados.filter((e) => permitidos.has(e.id))
}
