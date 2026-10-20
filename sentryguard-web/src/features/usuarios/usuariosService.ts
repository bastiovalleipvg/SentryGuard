import { useQuery } from '@tanstack/react-query'
import { AnalistasSchema, type ItemCatalogo } from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

export const usuariosService = {
  analistas(signal?: AbortSignal): Promise<ItemCatalogo[]> {
    return apiRequest('/api/v1/usuarios/analistas', { schema: AnalistasSchema, signal })
  },
}

export function useAnalistas(habilitado: boolean) {
  return useQuery({
    queryKey: ['usuarios', 'analistas'],
    queryFn: ({ signal }) => usuariosService.analistas(signal),
    enabled: habilitado,
  })
}
