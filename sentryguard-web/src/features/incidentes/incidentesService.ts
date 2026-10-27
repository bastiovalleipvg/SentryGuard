import {
  CreadoSchema,
  CumplimientoSlaSchema,
  IncidenteSchema,
  ListaBitacoraSchema,
  ListaEvidenciasSchema,
  PaginaIncidentesSchema,
  type ActualizarIncidenteInput,
  type CrearIncidenteInput,
  type CumplimientoSla,
  type EntradaBitacora,
  type Evidencia,
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
      query: {
        page: filtros.page,
        pageSize: TAMANO_PAGINA,
        q: filtros.q,
        estadoId: filtros.estadoId,
        severidadId: filtros.severidadId,
        activoId: filtros.activoId,
      },
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

  // Tablas relacionadas del incidente: bitacora (tb_BitacoraIncidente), evidencias y cumplimiento del SLA.
  async bitacora(id: number, signal?: AbortSignal): Promise<EntradaBitacora[]> {
    const r = await apiRequest(`${BASE}/${id}/bitacora`, { schema: ListaBitacoraSchema, signal })
    return r.items
  },

  async agregarBitacora(id: number, input: { accion: string; comentario: string | null }): Promise<void> {
    await apiRequest(`${BASE}/${id}/bitacora`, { method: 'POST', body: input })
  },

  async evidencias(id: number, signal?: AbortSignal): Promise<Evidencia[]> {
    const r = await apiRequest(`${BASE}/${id}/evidencia`, { schema: ListaEvidenciasSchema, signal })
    return r.items
  },

  async agregarEvidencia(id: number, input: { nombreArchivo: string; rutaAlmacenamiento: string }): Promise<void> {
    await apiRequest(`${BASE}/${id}/evidencia`, { method: 'POST', body: input })
  },

  cumplimientoSla(id: number, signal?: AbortSignal): Promise<CumplimientoSla> {
    return apiRequest(`${BASE}/sla/cumplimiento/${id}`, { schema: CumplimientoSlaSchema, signal })
  },
}
