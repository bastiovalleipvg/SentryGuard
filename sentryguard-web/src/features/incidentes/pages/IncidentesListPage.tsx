import { Link, useSearchParams } from 'react-router'
import type { FiltrosIncidentes } from '../../../api/contracts'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState } from '../../../components/ErrorState'
import { LoadingState } from '../../../components/LoadingState'
import { PageHeading } from '../../../components/PageHeading'
import { Pagination } from '../../../components/Pagination'
import { useMediaQuery } from '../../../hooks/useMediaQuery'
import { puede } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { IncidentesCards } from '../components/IncidentesCards'
import { IncidentesFilters, type ValoresFiltro } from '../components/IncidentesFilters'
import { IncidentesTable } from '../components/IncidentesTable'
import { useIncidentes } from '../queries'

function aEntero(valor: string | null): number | undefined {
  if (!valor || !/^\d+$/.test(valor)) return undefined
  return Number(valor)
}

export function IncidentesListPage() {
  const { usuario } = useAuth()
  const [params, setParams] = useSearchParams()
  // Tabla solo en escritorio: con 6 columnas, bajo 1024 px se lee mejor como tarjetas.
  const esPantallaAncha = useMediaQuery('(min-width: 64rem)')

  // Los filtros viven en la URL: se pueden compartir, recargar y navegar con Atras.
  const filtros: FiltrosIncidentes = {
    page: aEntero(params.get('page')) ?? 1,
    q: params.get('q') ?? undefined,
    estadoId: aEntero(params.get('estadoId')),
    severidadId: aEntero(params.get('severidadId')),
  }
  const consulta = useIncidentes(filtros)

  const aplicarFiltros = (v: ValoresFiltro) => {
    const siguiente = new URLSearchParams()
    if (v.q) siguiente.set('q', v.q)
    if (v.estadoId) siguiente.set('estadoId', v.estadoId)
    if (v.severidadId) siguiente.set('severidadId', v.severidadId)
    setParams(siguiente)
  }

  const cambiarPagina = (pagina: number) => {
    const siguiente = new URLSearchParams(params)
    siguiente.set('page', String(pagina))
    setParams(siguiente)
  }

  const hayFiltros = Boolean(filtros.q || filtros.estadoId || filtros.severidadId)
  const datos = consulta.data

  return (
    <>
      <div className="encabezado-pagina">
        <PageHeading>Incidentes</PageHeading>
        {puede(usuario, 'crearIncidente') && (
          <Link to="/incidentes/nuevo" className="boton boton--primario">
            <span aria-hidden="true">+ </span>Nuevo incidente
          </Link>
        )}
      </div>

      <IncidentesFilters
        key={params.toString()}
        valores={{ q: filtros.q ?? '', estadoId: params.get('estadoId') ?? '', severidadId: params.get('severidadId') ?? '' }}
        onAplicar={aplicarFiltros}
      />

      <p className="resultado-busqueda" role="status" aria-live="polite">
        {datos && `${datos.total} ${datos.total === 1 ? 'incidente encontrado' : 'incidentes encontrados'}.`}
      </p>

      {consulta.isPending && <LoadingState mensaje="Cargando incidentes…" />}

      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}

      {datos && datos.items.length === 0 && (
        <EmptyState titulo={hayFiltros ? 'Ningún incidente coincide con los filtros' : 'Todavía no hay incidentes'}>
          {hayFiltros ? (
            <p>Prueba con otros criterios o limpia los filtros.</p>
          ) : (
            puede(usuario, 'crearIncidente') && <p><Link to="/incidentes/nuevo">Registrar el primer incidente</Link></p>
          )}
        </EmptyState>
      )}

      {datos && datos.items.length > 0 && (
        <div aria-busy={consulta.isFetching}>
          {esPantallaAncha ? <IncidentesTable incidentes={datos.items} /> : <IncidentesCards incidentes={datos.items} />}
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={cambiarPagina} />
        </div>
      )}
    </>
  )
}
