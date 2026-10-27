import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ActualizarIncidenteInput, CrearIncidenteInput, FiltrosIncidentes } from '../../api/contracts'
import { incidentesService } from './incidentesService'

export const incidentesKeys = {
  todos: ['incidentes'] as const,
  lista: (filtros: FiltrosIncidentes) => ['incidentes', 'lista', filtros] as const,
  detalle: (id: number) => ['incidentes', 'detalle', id] as const,
  bitacora: (id: number) => ['incidentes', 'bitacora', id] as const,
  evidencias: (id: number) => ['incidentes', 'evidencias', id] as const,
  sla: (id: number) => ['incidentes', 'sla', id] as const,
}

export function useIncidentes(filtros: FiltrosIncidentes, habilitado = true) {
  return useQuery({
    queryKey: incidentesKeys.lista(filtros),
    queryFn: ({ signal }) => incidentesService.listar(filtros, signal),
    enabled: habilitado,
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

// Las acciones del flujo (editar, cambiar estado, asignar) agregan entradas automaticas a la bitacora:
// por eso las mutaciones invalidan todo el prefijo ['incidentes'] y la bitacora se recarga sola.

export function useBitacora(id: number) {
  return useQuery({ queryKey: incidentesKeys.bitacora(id), queryFn: ({ signal }) => incidentesService.bitacora(id, signal) })
}

export function useAgregarBitacora(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { accion: string; comentario: string | null }) => incidentesService.agregarBitacora(id, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: incidentesKeys.bitacora(id) }),
  })
}

export function useEvidencias(id: number) {
  return useQuery({ queryKey: incidentesKeys.evidencias(id), queryFn: ({ signal }) => incidentesService.evidencias(id, signal) })
}

export function useAgregarEvidencia(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { nombreArchivo: string; rutaAlmacenamiento: string }) => incidentesService.agregarEvidencia(id, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: incidentesKeys.evidencias(id) }),
  })
}

export function useCumplimientoSla(id: number) {
  return useQuery({ queryKey: incidentesKeys.sla(id), queryFn: ({ signal }) => incidentesService.cumplimientoSla(id, signal) })
}
