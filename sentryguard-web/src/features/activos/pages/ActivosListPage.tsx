import { Link } from 'react-router'
import { Icono } from '../../../components/Icono'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState } from '../../../components/ErrorState'
import { FiltrosListado, type CampoFiltro } from '../../../components/FiltrosListado'
import { ListadoResponsivo } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { PageHeading } from '../../../components/PageHeading'
import { Pagination } from '../../../components/Pagination'
import { useParametrosListado } from '../../../hooks/useParametrosListado'
import { puede } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { useCategorias } from '../../catalogos/catalogosService'
import { useOpcionesOrganizaciones } from '../../organizaciones/organizacionesService'
import { useListaActivos } from '../activosService'

type Filtro = 'q' | 'categoriaId' | 'organizacionId'

export function ActivosListPage() {
  const { usuario } = useAuth()
  const esStaff = puede(usuario, 'verOrganizaciones')
  const { filtros, valores, aplicar, cambiarPagina, hayFiltros, claveFormulario } = useParametrosListado<Filtro>(
    ['q', 'categoriaId', 'organizacionId'],
    ['categoriaId', 'organizacionId'],
  )
  const consulta = useListaActivos(filtros)
  const categorias = useCategorias()
  const organizaciones = useOpcionesOrganizaciones(esStaff)

  const campos: CampoFiltro<Filtro>[] = [
    { nombre: 'q', etiqueta: 'Buscar por nombre o IP', tipo: 'busqueda' },
    {
      nombre: 'categoriaId',
      etiqueta: 'Categoría',
      tipo: 'seleccion',
      textoTodos: 'Todas',
      opciones: categorias.data?.map((c) => ({ valor: String(c.id), texto: c.nombre })) ?? [],
    },
  ]
  // Un Cliente solo ve su organizacion: el filtro no tendria sentido (y la API lo ignora).
  if (esStaff) {
    campos.push({
      nombre: 'organizacionId',
      etiqueta: 'Organización',
      tipo: 'seleccion',
      textoTodos: 'Todas',
      opciones: organizaciones.data?.map((o) => ({ valor: String(o.id), texto: o.razonSocial })) ?? [],
    })
  }

  const datos = consulta.data

  return (
    <>
      <div className="encabezado-pagina">
        <PageHeading>Activos</PageHeading>
        {puede(usuario, 'crearActivo') && (
          <Link to="/activos/nuevo" className="boton boton--primario"><Icono nombre="mas" />Nuevo activo</Link>
        )}
      </div>
      {!esStaff && <p className="introduccion">Activos registrados para tu organización.</p>}

      <FiltrosListado key={claveFormulario} etiqueta="Filtrar activos" campos={campos} valores={valores} onAplicar={aplicar} />

      <p className="resultado-busqueda" role="status" aria-live="polite">
        {datos && `${datos.total} ${datos.total === 1 ? 'activo encontrado' : 'activos encontrados'}.`}
      </p>

      {consulta.isPending && <LoadingState mensaje="Cargando activos…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}

      {datos && datos.items.length === 0 && (
        <EmptyState titulo={hayFiltros ? 'Ningún activo coincide con los filtros' : 'Todavía no hay activos'}>
          {hayFiltros && <p>Prueba con otros criterios o limpia los filtros.</p>}
        </EmptyState>
      )}

      {datos && datos.items.length > 0 && (
        <div aria-busy={consulta.isFetching}>
          <ListadoResponsivo
            items={datos.items}
            etiqueta="Listado de activos"
            clave={(a) => a.id}
            principal={{ encabezado: 'Nombre', render: (a) => <Link to={`/activos/${a.id}`}>{a.nombre}</Link> }}
            columnas={[
              { encabezado: 'Hostname o IP', render: (a) => a.hostnameIp ?? 'Sin dato' },
              { encabezado: 'Categoría', render: (a) => a.categoria },
              { encabezado: 'Organización', render: (a) => a.organizacion },
            ]}
          />
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={cambiarPagina} etiqueta="Paginación de activos" />
        </div>
      )}
    </>
  )
}
