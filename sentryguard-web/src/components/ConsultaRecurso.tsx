import type { UseQueryResult } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { isApiError } from '../api/apiError'
import { AccessDeniedContent } from '../pages/AccessDeniedPage'
import { NotFoundContent } from '../pages/NotFoundPage'
import { ErrorState } from './ErrorState'
import { LoadingState } from './LoadingState'
import { PageHeading } from './PageHeading'

interface Props<T> {
  consulta: UseQueryResult<T>
  /** Encabezado que se muestra si la carga falla por un motivo distinto de 403 o 404. */
  titulo: string
  mensajeCarga: string
  children: (datos: T) => ReactNode
}

/**
 * Estados de una vista de detalle: carga, 403 (acceso denegado: la API rechazo el recurso por su
 * organizacion o asignacion), 404 y error generico con opcion de reintentar.
 */
export function ConsultaRecurso<T>({ consulta, titulo, mensajeCarga, children }: Props<T>) {
  if (consulta.isPending) return <LoadingState mensaje={mensajeCarga} />
  if (consulta.isError) {
    if (isApiError(consulta.error) && consulta.error.status === 403) return <AccessDeniedContent />
    if (isApiError(consulta.error) && consulta.error.status === 404) return <NotFoundContent />
    return (
      <>
        <PageHeading>{titulo}</PageHeading>
        <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />
      </>
    )
  }
  return <>{children(consulta.data)}</>
}
