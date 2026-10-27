import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import type { Activo } from '../../../api/contracts'
import { ConfirmDialog } from '../../../components/ConfirmDialog'
import { ConsultaRecurso } from '../../../components/ConsultaRecurso'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState, mensajeDeError } from '../../../components/ErrorState'
import { ListadoResponsivo } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { Pagination } from '../../../components/Pagination'
import { NotFoundContent } from '../../../pages/NotFoundPage'
import { formatearFecha } from '../../../utils/formato'
import { puede } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { EstadoBadge, SeveridadBadge } from '../../incidentes/components/Badges'
import { useIncidentes } from '../../incidentes/queries'
import { useActivo, useEliminarActivo } from '../activosService'

export function ActivoDetailPage() {
  const id = Number(useParams().id)
  const consulta = useActivo(id)
  if (!Number.isInteger(id) || id <= 0) return <NotFoundContent />
  return (
    <ConsultaRecurso consulta={consulta} titulo="Activo" mensajeCarga="Cargando activo…">
      {(activo) => <DetalleActivo activo={activo} />}
    </ConsultaRecurso>
  )
}

function DetalleActivo({ activo }: { activo: Activo }) {
  const { usuario } = useAuth()
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const [confirmando, setConfirmando] = useState(false)
  const eliminar = useEliminarActivo()

  const confirmarEliminacion = () => {
    eliminar.mutate(activo.id, {
      onSuccess: () => {
        setConfirmando(false)
        notificar(`Activo “${activo.nombre}” eliminado.`)
        navigate('/activos', { replace: true })
      },
      onError: (error) => {
        // 409: el activo tiene incidentes; la API explica el motivo en el mensaje.
        setConfirmando(false)
        notificar(mensajeDeError(error), 'error')
      },
    })
  }

  return (
    <>
      <p className="miga"><Link to="/activos"><span aria-hidden="true">←</span>Volver a activos</Link></p>

      <div className="encabezado-pagina">
        <PageHeading tituloDocumento={`Activo ${activo.nombre}`}>{activo.nombre}</PageHeading>
        <div className="acciones">
          {puede(usuario, 'crearIncidente') && (
            <Link to={`/incidentes/nuevo?activoId=${activo.id}`} className="boton boton--primario">Registrar incidente</Link>
          )}
          {puede(usuario, 'editarActivo') && (
            <Link to={`/activos/${activo.id}/editar`} className="boton boton--secundario">Editar</Link>
          )}
          {puede(usuario, 'eliminarActivo') && (
            <button type="button" className="boton boton--peligro" onClick={() => setConfirmando(true)}>Eliminar</button>
          )}
        </div>
      </div>

      <section aria-labelledby="titulo-datos-activo" className="panel">
        <h2 id="titulo-datos-activo">Datos del activo</h2>
        <dl className="datos">
          <div><dt>Hostname o IP</dt><dd>{activo.hostnameIp ?? 'Sin dato'}</dd></div>
          <div><dt>Categoría</dt><dd>{activo.categoria}</dd></div>
          <div>
            <dt>Organización</dt>
            <dd>
              {puede(usuario, 'verOrganizaciones') || usuario?.organizacionId === activo.organizacionId
                ? <Link to={`/organizaciones/${activo.organizacionId}`}>{activo.organizacion}</Link>
                : activo.organizacion}
            </dd>
          </div>
        </dl>
      </section>

      <IncidentesDelActivo activoId={activo.id} />

      <ConfirmDialog
        abierto={confirmando}
        titulo="¿Eliminar este activo?"
        mensaje={`Se eliminará “${activo.nombre}”. Si tiene incidentes registrados, la API rechazará la eliminación.`}
        textoConfirmar="Sí, eliminar"
        procesando={eliminar.isPending}
        onConfirmar={confirmarEliminacion}
        onCancelar={() => setConfirmando(false)}
      />
    </>
  )
}

// Relacion activo → incidentes. La API aplica el mismo filtro por rol que en el listado general.
function IncidentesDelActivo({ activoId }: { activoId: number }) {
  const [pagina, setPagina] = useState(1)
  const consulta = useIncidentes({ page: pagina, activoId })
  const datos = consulta.data

  return (
    <section aria-labelledby="titulo-incidentes-activo" className="panel">
      <h2 id="titulo-incidentes-activo">Incidentes de este activo</h2>
      {consulta.isPending && <LoadingState mensaje="Cargando incidentes…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}
      {datos?.items.length === 0 && <EmptyState titulo="Este activo no tiene incidentes que puedas ver" />}
      {datos && datos.items.length > 0 && (
        <>
          <ListadoResponsivo
            items={datos.items}
            etiqueta="Incidentes del activo"
            nivelTitulo={3}
            clave={(i) => i.id}
            principal={{ encabezado: 'Título', render: (i) => <Link to={`/incidentes/${i.id}`}>{i.titulo}</Link> }}
            columnas={[
              { encabezado: 'Severidad', render: (i) => <SeveridadBadge severidad={i.severidad} /> },
              { encabezado: 'Estado', render: (i) => <EstadoBadge estado={i.estado} /> },
              { encabezado: 'Creado', render: (i) => formatearFecha(i.fechaCreacion) },
            ]}
          />
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={setPagina} etiqueta="Paginación de incidentes del activo" />
        </>
      )}
    </section>
  )
}
