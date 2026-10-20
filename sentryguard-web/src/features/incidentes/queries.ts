import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ActualizarIncidenteInput, CrearIncidenteInput, FiltrosIncidentes } from '../../api/contracts'
import { incidentesService } from './incidentesService'

export const incidentesKeys = {
  todos: ['incidentes'] as const,
  lista: (filtros: FiltrosIncidentes) => ['incidentes', 'lista', filtros] as const,
  detalle: (id: number) => ['incidentes', 'detalle', id] as const,
}

export function useIncidentes(filtros: FiltrosIncidentes) {
  return useQuery({
    queryKey: incidentesKeys.lista(filtros),
    queryFn: ({ signal }) => incidentesService.listar(filtros, signal),
    // Mantiene la pagina anterior visible mientras carga la siguiente (sin parpadeo).
    placeholderData: keepPreviousData,
  })
}

export function useIncidente(id: number) {
  return useQuery({
    queryKey: incidentesKeys.detalle(id),
    queryFn: ({ signal }) => incidentesService.obtener(id, signal),
    enabled: Number.isInteger(id) && id > 0,
  })
}

function useInvalidarIncidentes() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: incidentesKeys.todos })
}

export function useCrearIncidente() {
  const invalidar = useInvalidarIncidentes()
  return useMutation({
    mutationFn: (input: CrearIncidenteInput) => incidentesService.crear(input),
    onSuccess: invalidar,
  })
}

export function useActualizarIncidente(id: number) {
  const invalidar = useInvalidarIncidentes()
  return useMutation({
    mutationFn: (input: ActualizarIncidenteInput) => incidentesService.actualizar(id, input),
    onSuccess: invalidar,
  })
}

export function useEliminarIncidente() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => incidentesService.eliminar(id),
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: incidentesKeys.detalle(id) })
      return queryClient.invalidateQueries({ queryKey: incidentesKeys.todos })
    },
  })
}

export function useAsignarAnalista(id: number) {
  const invalidar = useInvalidarIncidentes()
  return useMutation({
    mutationFn: (analistaId: number) => incidentesService.asignarAnalista(id, analistaId),
    onSuccess: invalidar,
  })
}

export function useCambiarEstado(id: number) {
  const invalidar = useInvalidarIncidentes()
  return useMutation({
    mutationFn: (estadoId: number) => incidentesService.cambiarEstado(id, estadoId),
    onSuccess: invalidar,
  })
}
