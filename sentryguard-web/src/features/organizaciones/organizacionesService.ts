import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CreadoSchema,
  ListaContratosSlaSchema,
  OrganizacionSchema,
  PaginaOrganizacionesSchema,
  type ContratoSla,
  type FiltrosListado,
  type Organizacion,
  type Pagina,
} from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

const BASE = '/api/v1/organizaciones'
export const TAMANO_PAGINA_ORGANIZACIONES = 10

export interface OrganizacionInput {
  razonSocial: string
  rut: string
  planId: number
}

export interface ContratoSlaInput {
  vigenteDesde: string
  vigenteHasta: string | null
  detalles: { severidadId: number; horasSla: number }[]
}

export const organizacionesService = {
  listar(filtros: FiltrosListado, signal?: AbortSignal): Promise<Pagina<Organizacion>> {
    return apiRequest(BASE, { query: { pageSize: TAMANO_PAGINA_ORGANIZACIONES, ...filtros }, schema: PaginaOrganizacionesSchema, signal })
  },

  async opciones(signal?: AbortSignal): Promise<Organizacion[]> {
    const r = await apiRequest(BASE, { query: { pageSize: 50 }, schema: PaginaOrganizacionesSchema, signal })
    return r.items
  },

  obtener(id: number, signal?: AbortSignal): Promise<Organizacion> {
    return apiRequest(`${BASE}/${id}`, { schema: OrganizacionSchema, signal })
  },

  crear(input: OrganizacionInput): Promise<Organizacion> {
    return apiRequest(BASE, { method: 'POST', body: input, schema: OrganizacionSchema })
  },

  async actualizar(id: number, input: OrganizacionInput): Promise<void> {
    await apiRequest(`${BASE}/${id}`, { method: 'PUT', body: input })
  },

  async eliminar(id: number): Promise<void> {
    await apiRequest(`${BASE}/${id}`, { method: 'DELETE' })
  },

  async contratos(id: number, signal?: AbortSignal): Promise<ContratoSla[]> {
    const r = await apiRequest(`${BASE}/${id}/contratos-sla`, { schema: ListaContratosSlaSchema, signal })
    return r.items
  },

  async crearContrato(id: number, input: ContratoSlaInput): Promise<number> {
    const r = await apiRequest(`${BASE}/${id}/contratos-sla`, { method: 'POST', body: input, schema: CreadoSchema })
    return r.id
  },

  async finalizarContrato(id: number, contratoId: number): Promise<void> {
    await apiRequest(`${BASE}/${id}/contratos-sla/${contratoId}/finalizar`, { method: 'POST' })
  },

  async eliminarContrato(id: number, contratoId: number): Promise<void> {
    await apiRequest(`${BASE}/${id}/contratos-sla/${contratoId}`, { method: 'DELETE' })
  },
}

export const organizacionesKeys = {
  todos: ['organizaciones'] as const,
  lista: (filtros: FiltrosListado) => ['organizaciones', 'lista', filtros] as const,
  opciones: ['organizaciones', 'opciones'] as const,
  detalle: (id: number) => ['organizaciones', 'detalle', id] as const,
  contratos: (id: number) => ['organizaciones', 'contratos', id] as const,
}

export function useListaOrganizaciones(filtros: FiltrosListado) {
  return useQuery({
    queryKey: organizacionesKeys.lista(filtros),
    queryFn: ({ signal }) => organizacionesService.listar(filtros, signal),
    placeholderData: keepPreviousData,
  })
}

/** Organizaciones para selectores; solo el staff puede listarlas (un Cliente recibiria 403). */
export function useOpcionesOrganizaciones(habilitado: boolean) {
  return useQuery({
    queryKey: organizacionesKeys.opciones,
    queryFn: ({ signal }) => organizacionesService.opciones(signal),
    enabled: habilitado,
  })
}

export function useOrganizacion(id: number) {
  return useQuery({
    queryKey: organizacionesKeys.detalle(id),
    queryFn: ({ signal }) => organizacionesService.obtener(id, signal),
    enabled: Number.isInteger(id) && id > 0,
  })
}

export function useContratosSla(id: number) {
  return useQuery({
    queryKey: organizacionesKeys.contratos(id),
    queryFn: ({ signal }) => organizacionesService.contratos(id, signal),
  })
}

function useInvalidarOrganizaciones() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: organizacionesKeys.todos })
}

export function useCrearOrganizacion() {
  const invalidar = useInvalidarOrganizaciones()
  return useMutation({ mutationFn: organizacionesService.crear, onSuccess: invalidar })
}

export function useActualizarOrganizacion(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: OrganizacionInput) => organizacionesService.actualizar(id, input),
    // La razon social aparece en los activos y usuarios: se refrescan tambien.
    onSuccess: () => Promise.all(['organizaciones', 'activos', 'usuarios'].map((k) => queryClient.invalidateQueries({ queryKey: [k] }))),
  })
}

export function useEliminarOrganizacion() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: organizacionesService.eliminar,
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: organizacionesKeys.detalle(id) })
      return queryClient.invalidateQueries({ queryKey: organizacionesKeys.todos })
    },
  })
}

export function useMutacionesContrato(organizacionId: number) {
  const queryClient = useQueryClient()
  const invalidar = () => queryClient.invalidateQueries({ queryKey: organizacionesKeys.contratos(organizacionId) })
  return {
    crear: useMutation({ mutationFn: (input: ContratoSlaInput) => organizacionesService.crearContrato(organizacionId, input), onSuccess: invalidar }),
    finalizar: useMutation({ mutationFn: (contratoId: number) => organizacionesService.finalizarContrato(organizacionId, contratoId), onSuccess: invalidar }),
    eliminar: useMutation({ mutationFn: (contratoId: number) => organizacionesService.eliminarContrato(organizacionId, contratoId), onSuccess: invalidar }),
  }
}
