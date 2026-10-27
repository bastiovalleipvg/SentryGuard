import { Link } from 'react-router'
import { Icono } from '../../../components/Icono'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState } from '../../../components/ErrorState'
import { FiltrosListado } from '../../../components/FiltrosListado'
import { ListadoResponsivo } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { PageHeading } from '../../../components/PageHeading'
import { Pagination } from '../../../components/Pagination'
import { useParametrosListado } from '../../../hooks/useParametrosListado'
import { formatearDia } from '../../../utils/formato'
import { puede } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { usePlanes } from '../../catalogos/catalogosService'
import { useListaOrganizaciones } from '../organizacionesService'

export function OrganizacionesListPage() {
  const { usuario } = useAuth()
  const { filtros, valores, aplicar, cambiarPagina, hayFiltros, claveFormulario } = useParametrosListado(['q', 'planId'] as const, ['planId'])
  const consulta = useListaOrganizaciones(filtros)
  const planes = usePlanes()
  const datos = consulta.data

  return (
    <>
      <div className="encabezado-pagina">
        <PageHeading>Organizaciones</PageHeading>
        {puede(usuario, 'gestionarOrganizaciones') && (
          <Link to="/organizaciones/nueva" className="boton boton--primario"><Icono nombre="mas" />Nueva organización</Link>
        )}
      </div>
      <p className="introduccion">Empresas clientes de CyberOps Chile con su plan de servicio contratado.</p>

      <FiltrosListado
        key={claveFormulario}
        etiqueta="Filtrar organizaciones"
        valores={valores}
        onAplicar={aplicar}
        campos={[
          { nombre: 'q', etiqueta: 'Buscar por razón social o RUT', tipo: 'busqueda' },
          { nombre: 'planId', etiqueta: 'Plan', tipo: 'seleccion', opciones: planes.data?.map((p) => ({ valor: String(p.id), texto: p.nombre })) ?? [] },
        ]}
      />

      <p className="resultado-busqueda" role="status" aria-live="polite">
        {datos && `${datos.total} ${datos.total === 1 ? 'organización encontrada' : 'organizaciones encontradas'}.`}
      </p>

      {consulta.isPending && <LoadingState mensaje="Cargando organizaciones…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}
      {datos && datos.items.length === 0 && (
        <EmptyState titulo={hayFiltros ? 'Ninguna organización coincide con los filtros' : 'Todavía no hay organizaciones'} />
      )}

      {datos && datos.items.length > 0 && (
        <div aria-busy={consulta.isFetching}>
          <ListadoResponsivo
            items={datos.items}
            etiqueta="Listado de organizaciones"
            clave={(o) => o.id}
            principal={{ encabezado: 'Razón social', render: (o) => <Link to={`/organizaciones/${o.id}`}>{o.razonSocial}</Link> }}
            columnas={[
              { encabezado: 'RUT', render: (o) => o.rut },
              { encabezado: 'Plan', render: (o) => o.plan },
              { encabezado: 'Cliente desde', render: (o) => formatearDia(o.fechaAlta) },
            ]}
          />
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={cambiarPagina} etiqueta="Paginación de organizaciones" />
        </div>
      )}
    </>
  )
}
