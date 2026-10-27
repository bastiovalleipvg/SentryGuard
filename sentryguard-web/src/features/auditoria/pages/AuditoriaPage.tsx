import { EmptyState } from '../../../components/EmptyState'
import { ErrorState } from '../../../components/ErrorState'
import { FiltrosListado } from '../../../components/FiltrosListado'
import { ListadoResponsivo } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { PageHeading } from '../../../components/PageHeading'
import { Pagination } from '../../../components/Pagination'
import { useParametrosListado } from '../../../hooks/useParametrosListado'
import { formatearFecha } from '../../../utils/formato'
import { useAuditoria } from '../auditoriaService'

// Registro de auditoria (tb_LogAuditoria): cada escritura exitosa en la API queda con usuario, ruta, IP y hora.
export function AuditoriaPage() {
  const { filtros, valores, aplicar, cambiarPagina, hayFiltros, claveFormulario } = useParametrosListado(['q'] as const)
  const consulta = useAuditoria(filtros)
  const datos = consulta.data

  return (
    <>
      <PageHeading>Auditoría</PageHeading>
      <p className="introduccion">Escrituras realizadas en la API (crear, editar, eliminar), de la más reciente a la más antigua. Solo lectura.</p>

      <FiltrosListado
        key={claveFormulario}
        etiqueta="Filtrar auditoría"
        valores={valores}
        onAplicar={aplicar}
        campos={[{ nombre: 'q', etiqueta: 'Buscar en la acción (por ejemplo: DELETE o /activos)', tipo: 'busqueda' }]}
      />

      <p className="resultado-busqueda" role="status" aria-live="polite">
        {datos && `${datos.total} ${datos.total === 1 ? 'registro' : 'registros'}.`}
      </p>

      {consulta.isPending && <LoadingState mensaje="Cargando auditoría…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}
      {datos && datos.items.length === 0 && <EmptyState titulo={hayFiltros ? 'Ningún registro coincide con la búsqueda' : 'Todavía no hay registros'} />}

      {datos && datos.items.length > 0 && (
        <div aria-busy={consulta.isFetching}>
          <ListadoResponsivo
            items={datos.items}
            etiqueta="Registro de auditoría"
            clave={(r) => r.id}
            principal={{ encabezado: 'Acción', render: (r) => <code>{r.accion}</code> }}
            columnas={[
              { encabezado: 'Usuario', render: (r) => r.usuario ?? 'Usuario eliminado' },
              { encabezado: 'IP de origen', render: (r) => r.ipOrigen ?? 'Sin dato' },
              { encabezado: 'Fecha', render: (r) => formatearFecha(r.timestamp) },
            ]}
          />
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={cambiarPagina} etiqueta="Paginación de auditoría" />
        </div>
      )}
    </>
  )
}
