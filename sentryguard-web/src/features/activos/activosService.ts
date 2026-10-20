import { useQuery } from '@tanstack/react-query'
import { PaginaActivosSchema, type Activo } from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

export const activosService = {
  // La API ya filtra por organizacion segun el rol (un Cliente solo recibe sus propios activos).
  async listar(signal?: AbortSignal): Promise<Activo[]> {
    const r = await apiRequest('/api/v1/activos', { query: { pageSize: 50 }, schema: PaginaActivosSchema, signal })
    return r.items
  },
}

export function useActivos(habilitado = true) {
  return useQuery({
    queryKey: ['activos'],
    queryFn: ({ signal }) => activosService.listar(signal),
    enabled: habilitado,
  })
}
