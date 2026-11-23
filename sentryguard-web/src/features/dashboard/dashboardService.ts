import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { IndicadoresSchema, SeriesDashboardSchema, type FiltrosListado, type Indicadores, type SeriesDashboard } from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

const BASE = '/api/v1/dashboard'

type FiltrosDashboard = Omit<FiltrosListado, 'page'>

export const dashboardService = {
  indicadores(filtros: FiltrosDashboard, signal?: AbortSignal): Promise<Indicadores> {
    return apiRequest(`${BASE}/indicadores`, { query: filtros, schema: IndicadoresSchema, signal })
  },
  series(filtros: FiltrosDashboard, signal?: AbortSignal): Promise<SeriesDashboard> {
    return apiRequest(`${BASE}/series`, { query: filtros, schema: SeriesDashboardSchema, signal })
  },
}

export function useIndicadores(filtros: FiltrosDashboard) {
  return useQuery({
    queryKey: ['dashboard', 'indicadores', filtros],
    queryFn: ({ signal }) => dashboardService.indicadores(filtros, signal),
    placeholderData: keepPreviousData,
  })
}

export function useSeries(filtros: FiltrosDashboard) {
  return useQuery({
    queryKey: ['dashboard', 'series', filtros],
    queryFn: ({ signal }) => dashboardService.series(filtros, signal),
    placeholderData: keepPreviousData,
  })
}
