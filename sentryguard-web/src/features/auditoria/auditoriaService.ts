import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { PaginaAuditoriaSchema, type FiltrosListado, type Pagina, type RegistroAuditoria } from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

export const TAMANO_PAGINA_AUDITORIA = 20

export const auditoriaService = {
  listar(filtros: FiltrosListado, signal?: AbortSignal): Promise<Pagina<RegistroAuditoria>> {
    return apiRequest('/api/v1/auditoria', { query: { pageSize: TAMANO_PAGINA_AUDITORIA, ...filtros }, schema: PaginaAuditoriaSchema, signal })
  },
}

export function useAuditoria(filtros: FiltrosListado) {
  return useQuery({
    queryKey: ['auditoria', filtros],
    queryFn: ({ signal }) => auditoriaService.listar(filtros, signal),
    placeholderData: keepPreviousData,
    // El registro cambia con cada escritura de cualquier usuario: no se reutiliza una copia vieja.
    staleTime: 0,
  })
}
