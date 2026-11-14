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

function DetalleCambios({ anteriores, nuevos }: { anteriores?: string | null; nuevos?: string | null }) {
  if (!anteriores && !nuevos) return <span className="meta">Sin cambios registrados</span>

  let antObj: Record<string, unknown> = {}
  let nueObj: Record<string, unknown> = {}
  try { if (anteriores) antObj = JSON.parse(anteriores) } catch { /* ignorar parse */ }
  try { if (nuevos) nueObj = JSON.parse(nuevos) } catch { /* ignorar parse */ }

  const campos = Array.from(new Set([...Object.keys(antObj), ...Object.keys(nueObj)]))
  if (campos.length === 0) return <span className="meta">Sin cambios registrados</span>

  return (
    <details className="auditoria-cambios">
      <summary style={{ cursor: 'pointer', color: 'var(--color-primario, #2563eb)' }}>Ver cambios ({campos.length})</summary>
      <ul style={{ margin: '0.5rem 0 0', paddingLeft: '1.25rem', fontSize: '0.8125rem', lineHeight: '1.4' }}>
        {campos.map((campo) => {
          const antes = antObj[campo] !== undefined ? String(antObj[campo]) : '—'
          const despues = nueObj[campo] !== undefined ? String(nueObj[campo]) : '—'
          return (
            <li key={campo}>
              <strong>{campo}:</strong> <code>{antes}</code> → <code>{despues}</code>
            </li>
          )
        })}
      </ul>
    </details>
  )
}

// Registro de auditoria (tb_LogAuditoria): cada escritura exitosa en la API queda con usuario, entidad, cambios, ruta, IP y hora.
export function AuditoriaPage() {
  const { filtros, valores, aplicar, cambiarPagina, hayFiltros, claveFormulario } = useParametrosListado(['q', 'entidad'] as const)
  const consulta = useAuditoria(filtros)
  const datos = consulta.data

  return (
    <>
      <PageHeading>Auditoría</PageHeading>
      <p className="introduccion">Escrituras realizadas en la API (crear, editar, eliminar), de la más reciente a la más antigua con detalle de valores anteriores y nuevos. Solo lectura.</p>

      <FiltrosListado
        key={claveFormulario}
        etiqueta="Filtrar auditoría"
        valores={valores}
        onAplicar={aplicar}
        campos={[
          { nombre: 'q', etiqueta: 'Buscar en la acción (por ejemplo: DELETE o /activos)', tipo: 'busqueda' },
          {
            nombre: 'entidad',
            etiqueta: 'Entidad afectada',
            tipo: 'seleccion',
            textoTodos: 'Todas las entidades',
            opciones: [
              { valor: 'Incidente', texto: 'Incidente' },
              { valor: 'Activo', texto: 'Activo' },
              { valor: 'Organizacion', texto: 'Organización' },
              { valor: 'Usuario', texto: 'Usuario' },
              { valor: 'CategoriaIncidente', texto: 'Categoría de incidente' },
              { valor: 'CategoriaActivo', texto: 'Categoría de activo' },
            ],
          },
        ]}
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
              { encabezado: 'Entidad', render: (r) => r.entidad ? `${r.entidad} #${r.entidadId ?? ''}` : 'General' },
              { encabezado: 'Detalle de cambios', render: (r) => <DetalleCambios anteriores={r.valoresAnteriores} nuevos={r.valoresNuevos} /> },
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
