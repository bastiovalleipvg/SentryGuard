import { useMutation } from '@tanstack/react-query'
import {
  ClasificacionIaSchema,
  ResumenIaSchema,
  type ClasificacionIa,
  type ResumenIa,
} from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

export const iaService = {
  clasificarIncidente(input: { titulo: string; descripcion: string }, signal?: AbortSignal): Promise<ClasificacionIa> {
    return apiRequest('/api/v1/ia/clasificar-incidente', {
      method: 'POST',
      body: input,
      schema: ClasificacionIaSchema,
      signal,
    })
  },

  resumir(input: { contexto: string; tipo?: string }, signal?: AbortSignal): Promise<ResumenIa> {
    return apiRequest('/api/v1/ia/resumir', {
      method: 'POST',
      body: input,
      schema: ResumenIaSchema,
      signal,
    })
  },
}

export function useClasificarIncidente() {
  return useMutation({
    mutationFn: (input: { titulo: string; descripcion: string }) => iaService.clasificarIncidente(input),
  })
}

export function useResumirIa() {
  return useMutation({
    mutationFn: (input: { contexto: string; tipo?: string }) => iaService.resumir(input),
  })
}
