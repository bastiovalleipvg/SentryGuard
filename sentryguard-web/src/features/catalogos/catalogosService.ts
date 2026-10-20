import { useQuery } from '@tanstack/react-query'
import { CatalogoSchema, CatalogoSeveridadesSchema, type ItemCatalogo, type Severidad } from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

// Recurso relacionado: catalogos usados en los formularios y filtros de incidentes.
export const catalogosService = {
  async severidades(signal?: AbortSignal): Promise<Severidad[]> {
    const r = await apiRequest('/api/v1/catalogos/severidades', { query: { pageSize: 50 }, schema: CatalogoSeveridadesSchema, signal })
    return r.items
  },

  async estados(signal?: AbortSignal): Promise<ItemCatalogo[]> {
    const r = await apiRequest('/api/v1/catalogos/estados-incidente', { query: { pageSize: 50 }, schema: CatalogoSchema, signal })
    return r.items.toSorted((a, b) => a.id - b.id)
  },
}

const UNA_HORA = 60 * 60 * 1000

export function useSeveridades() {
  return useQuery({
    queryKey: ['catalogos', 'severidades'],
    queryFn: ({ signal }) => catalogosService.severidades(signal),
    staleTime: UNA_HORA,
  })
}

export function useEstados() {
  return useQuery({
    queryKey: ['catalogos', 'estados'],
    queryFn: ({ signal }) => catalogosService.estados(signal),
    staleTime: UNA_HORA,
  })
}
