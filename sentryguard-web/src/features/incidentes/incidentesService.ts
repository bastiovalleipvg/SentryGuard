import {
  CreadoSchema,
  IncidenteSchema,
  PaginaIncidentesSchema,
  type ActualizarIncidenteInput,
  type CrearIncidenteInput,
  type FiltrosIncidentes,
  type Incidente,
  type PaginaIncidentes,
} from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

const BASE = '/api/v1/incidentes'
export const TAMANO_PAGINA = 10

export const incidentesService = {
  listar(filtros: FiltrosIncidentes, signal?: AbortSignal): Promise<PaginaIncidentes> {
    return apiRequest(BASE, {
      query: { page: filtros.page, pageSize: TAMANO_PAGINA, q: filtros.q, estadoId: filtros.estadoId, severidadId: filtros.severidadId },
      schema: PaginaIncidentesSchema,
      signal,
    })
  },

  obtener(id: number, signal?: AbortSignal): Promise<Incidente> {
    return apiRequest(`${BASE}/${id}`, { schema: IncidenteSchema, signal })
  },

  async crear(input: CrearIncidenteInput): Promise<number> {
    const creado = await apiRequest(BASE, { method: 'POST', body: input, schema: CreadoSchema })
    return creado.id
  },

  async actualizar(id: number, input: ActualizarIncidenteInput): Promise<void> {
    await apiRequest(`${BASE}/${id}`, { method: 'PUT', body: input })
  },

  async eliminar(id: number): Promise<void> {
    await apiRequest(`${BASE}/${id}`, { method: 'DELETE' })
  },

  async asignarAnalista(id: number, analistaId: number): Promise<void> {
    await apiRequest(`${BASE}/${id}/asignar`, { method: 'POST', body: { analistaId } })
  },

  async cambiarEstado(id: number, estadoId: number): Promise<void> {
    await apiRequest(`${BASE}/${id}/cambiar-estado`, { method: 'POST', body: { estadoId } })
  },
}
