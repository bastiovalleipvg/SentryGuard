import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query'
import { PaginaReporteSchema, type FiltrosListado, type PaginaReporte, type TipoReporte } from '../../api/contracts'
import { apiRequest, descargarArchivo } from '../../api/httpClient'

export type FormatoExportacion = 'xlsx' | 'pdf' | 'csv'

export const reportesService = {
  obtener(tipo: TipoReporte, filtros: FiltrosListado, signal?: AbortSignal): Promise<PaginaReporte> {
    return apiRequest(`/api/v1/reportes/${tipo}`, { query: filtros, schema: PaginaReporteSchema, signal })
  },

  exportar(tipo: TipoReporte, formato: FormatoExportacion, filtros: Omit<FiltrosListado, 'page'>) {
    return descargarArchivo(`/api/v1/reportes/${tipo}/exportar`, { ...filtros, formato })
  },
}

export function useReporte(tipo: TipoReporte, filtros: FiltrosListado) {
  return useQuery({
    queryKey: ['reportes', tipo, filtros],
    queryFn: ({ signal }) => reportesService.obtener(tipo, filtros, signal),
    placeholderData: keepPreviousData,
  })
}

/** Descarga el archivo en el navegador con el nombre que sugiere la API. */
export function useExportarReporte() {
  return useMutation({
    mutationFn: async (entrada: { tipo: TipoReporte; formato: FormatoExportacion; filtros: Omit<FiltrosListado, 'page'> }) => {
      const { blob, nombre } = await reportesService.exportar(entrada.tipo, entrada.formato, entrada.filtros)
      const url = URL.createObjectURL(blob)
      const enlace = document.createElement('a')
      enlace.href = url
      enlace.download = nombre
      document.body.append(enlace)
      enlace.click()
      enlace.remove()
      URL.revokeObjectURL(url)
      return nombre
    },
  })
}
