import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ActivoSchema, PaginaActivosSchema, type Activo, type FiltrosListado, type Pagina } from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

const BASE = '/api/v1/activos'
export const TAMANO_PAGINA_ACTIVOS = 10

export interface ActivoInput {
  nombre: string
  hostnameIp: string | null
  categoriaId: number
}

export const activosService = {
  // La API ya filtra por organizacion segun el rol (un Cliente solo recibe sus propios activos).
  listar(filtros: FiltrosListado, signal?: AbortSignal): Promise<Pagina<Activo>> {
    return apiRequest(BASE, { query: { pageSize: TAMANO_PAGINA_ACTIVOS, ...filtros }, schema: PaginaActivosSchema, signal })
  },

  /** Opciones para selectores (formulario de incidente): primera pagina de 50, ordenada por nombre. */
  async opciones(signal?: AbortSignal): Promise<Activo[]> {
    const r = await apiRequest(BASE, { query: { pageSize: 50 }, schema: PaginaActivosSchema, signal })
    return r.items
  },

  obtener(id: number, signal?: AbortSignal): Promise<Activo> {
    return apiRequest(`${BASE}/${id}`, { schema: ActivoSchema, signal })
  },

  async crear(input: ActivoInput & { organizacionId: number }): Promise<Activo> {
    return apiRequest(BASE, { method: 'POST', body: input, schema: ActivoSchema })
  },

  async actualizar(id: number, input: ActivoInput): Promise<void> {
    await apiRequest(`${BASE}/${id}`, { method: 'PUT', body: input })
  },

  async eliminar(id: number): Promise<void> {
    await apiRequest(`${BASE}/${id}`, { method: 'DELETE' })
  },
}

export const activosKeys = {
  todos: ['activos'] as const,
  lista: (filtros: FiltrosListado) => ['activos', 'lista', filtros] as const,
  opciones: ['activos', 'opciones'] as const,
  detalle: (id: number) => ['activos', 'detalle', id] as const,
}

export function useActivos(habilitado = true) {
  return useQuery({
    queryKey: activosKeys.opciones,
    queryFn: ({ signal }) => activosService.opciones(signal),
    enabled: habilitado,
  })
}

export function useListaActivos(filtros: FiltrosListado, habilitado = true) {
  return useQuery({
    queryKey: activosKeys.lista(filtros),
    queryFn: ({ signal }) => activosService.listar(filtros, signal),
    placeholderData: keepPreviousData,
    enabled: habilitado,
  })
}

export function useActivo(id: number) {
  return useQuery({
    queryKey: activosKeys.detalle(id),
    queryFn: ({ signal }) => activosService.obtener(id, signal),
    enabled: Number.isInteger(id) && id > 0,
  })
}

function useInvalidarActivos() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: activosKeys.todos })
}

export function useCrearActivo() {
  const invalidar = useInvalidarActivos()
  return useMutation({ mutationFn: activosService.crear, onSuccess: invalidar })
}

export function useActualizarActivo(id: number) {
  const invalidar = useInvalidarActivos()
  return useMutation({ mutationFn: (input: ActivoInput) => activosService.actualizar(id, input), onSuccess: invalidar })
}

export function useEliminarActivo() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: activosService.eliminar,
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: activosKeys.detalle(id) })
      return queryClient.invalidateQueries({ queryKey: activosKeys.todos })
    },
  })
}
