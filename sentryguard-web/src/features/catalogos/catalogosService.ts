import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CatalogoPlanesSchema,
  CatalogoSchema,
  CatalogoSeveridadesSchema,
  CreadoSchema,
  type ItemCatalogo,
  type PlanServicio,
  type Severidad,
} from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

const BASE = '/api/v1/catalogos'

// Recursos relacionados: catalogos usados en formularios y filtros, y administrados por el Administrador.
export const catalogosService = {
  async severidades(signal?: AbortSignal): Promise<Severidad[]> {
    const r = await apiRequest(`${BASE}/severidades`, { query: { pageSize: 50 }, schema: CatalogoSeveridadesSchema, signal })
    return r.items
  },

  async estados(signal?: AbortSignal): Promise<ItemCatalogo[]> {
    const r = await apiRequest(`${BASE}/estados-incidente`, { query: { pageSize: 50 }, schema: CatalogoSchema, signal })
    return r.items.toSorted((a, b) => a.id - b.id)
  },

  async categorias(signal?: AbortSignal): Promise<ItemCatalogo[]> {
    const r = await apiRequest(`${BASE}/categorias-activo`, { query: { pageSize: 50 }, schema: CatalogoSchema, signal })
    return r.items
  },

  async planes(signal?: AbortSignal): Promise<PlanServicio[]> {
    const r = await apiRequest(`${BASE}/planes-servicio`, { query: { pageSize: 50 }, schema: CatalogoPlanesSchema, signal })
    return r.items
  },

  async roles(signal?: AbortSignal): Promise<ItemCatalogo[]> {
    const r = await apiRequest(`${BASE}/roles`, { schema: CatalogoSchema, signal })
    return r.items
  },
}

/** Ruta de cada catalogo administrable en la API. */
export type TipoCatalogo = 'severidades' | 'estados-incidente' | 'categorias-activo' | 'planes-servicio'

export const catalogosAdminService = {
  async crear(tipo: TipoCatalogo, datos: Record<string, unknown>): Promise<number> {
    const r = await apiRequest(`${BASE}/${tipo}`, { method: 'POST', body: datos, schema: CreadoSchema })
    return r.id
  },

  async actualizar(tipo: TipoCatalogo, id: number, datos: Record<string, unknown>): Promise<void> {
    await apiRequest(`${BASE}/${tipo}/${id}`, { method: 'PUT', body: datos })
  },

  async eliminar(tipo: TipoCatalogo, id: number): Promise<void> {
    await apiRequest(`${BASE}/${tipo}/${id}`, { method: 'DELETE' })
  },
}

const UNA_HORA = 60 * 60 * 1000

export const catalogosKeys = {
  todos: ['catalogos'] as const,
  severidades: ['catalogos', 'severidades'] as const,
  estados: ['catalogos', 'estados'] as const,
  categorias: ['catalogos', 'categorias'] as const,
  planes: ['catalogos', 'planes'] as const,
  roles: ['catalogos', 'roles'] as const,
}

export function useSeveridades() {
  return useQuery({ queryKey: catalogosKeys.severidades, queryFn: ({ signal }) => catalogosService.severidades(signal), staleTime: UNA_HORA })
}

export function useEstados() {
  return useQuery({ queryKey: catalogosKeys.estados, queryFn: ({ signal }) => catalogosService.estados(signal), staleTime: UNA_HORA })
}

export function useCategorias() {
  return useQuery({ queryKey: catalogosKeys.categorias, queryFn: ({ signal }) => catalogosService.categorias(signal), staleTime: UNA_HORA })
}

export function usePlanes() {
  return useQuery({ queryKey: catalogosKeys.planes, queryFn: ({ signal }) => catalogosService.planes(signal), staleTime: UNA_HORA })
}

export function useRoles() {
  return useQuery({ queryKey: catalogosKeys.roles, queryFn: ({ signal }) => catalogosService.roles(signal), staleTime: UNA_HORA })
}

/** Mutaciones de un catalogo; al terminar se recargan todos los catalogos (los usan varios formularios). */
export function useMutacionesCatalogo(tipo: TipoCatalogo) {
  const queryClient = useQueryClient()
  const invalidar = () => queryClient.invalidateQueries({ queryKey: catalogosKeys.todos })
  return {
    crear: useMutation({ mutationFn: (datos: Record<string, unknown>) => catalogosAdminService.crear(tipo, datos), onSuccess: invalidar }),
    actualizar: useMutation({
      mutationFn: ({ id, datos }: { id: number; datos: Record<string, unknown> }) => catalogosAdminService.actualizar(tipo, id, datos),
      onSuccess: invalidar,
    }),
    eliminar: useMutation({ mutationFn: (id: number) => catalogosAdminService.eliminar(tipo, id), onSuccess: invalidar }),
  }
}
