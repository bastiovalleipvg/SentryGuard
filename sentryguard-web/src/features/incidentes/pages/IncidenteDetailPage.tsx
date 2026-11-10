import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { isApiError } from '../../../api/apiError'
import type { Incidente } from '../../../api/contracts'
import { ConfirmDialog } from '../../../components/ConfirmDialog'
import { ErrorState, mensajeDeError } from '../../../components/ErrorState'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { Icono } from '../../../components/Icono'
import { PageHeading } from '../../../components/PageHeading'
import { AccessDeniedContent } from '../../../pages/AccessDeniedPage'
import { NotFoundContent } from '../../../pages/NotFoundPage'
import { formatearFecha } from '../../../utils/formato'
import { puede, puedeSobreIncidente } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { useEstados } from '../../catalogos/catalogosService'
import { useAnalistas } from '../../usuarios/usuariosService'
import { EstadoBadge, SeveridadBadge } from '../components/Badges'
import { BitacoraPanel } from '../components/BitacoraPanel'
import { EvidenciasPanel } from '../components/EvidenciasPanel'
import { SlaPanel } from '../components/SlaPanel'
import { useAsignarAnalista, useCambiarEstado, useEliminarIncidente, useIncidente } from '../queries'
import { obtenerEstadosDestinoPermitidos } from '../transiciones'

export function IncidenteDetailPage() {
  const id = Number(useParams().id)
  const consulta = useIncidente(id)

  if (!Number.isInteger(id) || id <= 0) return <NotFoundContent />
  if (consulta.isPending) return <LoadingState mensaje="Cargando incidente…" />
  if (consulta.isError) {
    if (isApiError(consulta.error) && consulta.error.status === 403) return <AccessDeniedContent />
    if (isApiError(consulta.error) && consulta.error.status === 404) return <NotFoundContent />
    return (
      <>
        <PageHeading>Incidente</PageHeading>
        <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />
      </>
    )
  }
  return <DetalleIncidente incidente={consulta.data} />
}

function DetalleIncidente({ incidente }: { incidente: Incidente }) {
  const { usuario } = useAuth()
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const [confirmando, setConfirmando] = useState(false)
  const eliminar = useEliminarIncidente()

  const puedeEditar = puedeSobreIncidente(usuario, 'editarIncidente', incidente)
  const puedeEliminar = puede(usuario, 'eliminarIncidente')

  const confirmarEliminacion = () => {
    eliminar.mutate(incidente.id, {
      onSuccess: () => {
        setConfirmando(false)
        notificar(`Incidente “${incidente.titulo}” eliminado.`)
        navigate('/incidentes', { replace: true })
      },
      onError: (error) => {
        setConfirmando(false)
        notificar(mensajeDeError(error), 'error')
      },
    })
  }

  return (
    <>
      <p className="miga">
        <Link to="/incidentes"><Icono nombre="volver" />Volver al listado</Link>
      </p>

      <div className="encabezado-pagina">
        <div className="encabezado-pagina__texto">
          {/* El titulo viene de la API y se renderiza como texto: React escapa cualquier HTML o script. */}
          <PageHeading tituloDocumento={`Incidente #${incidente.id}`}>{incidente.titulo}</PageHeading>
          <p className="insignias">
            <SeveridadBadge severidad={incidente.severidad} />
            <EstadoBadge estado={incidente.estado} />
            <span className="meta">#{incidente.id} · creado el {formatearFecha(incidente.fechaCreacion)}</span>
          </p>
        </div>
        {(puedeEditar || puedeEliminar) && (
          <div className="acciones">
            {puedeEditar && (
              <Link to={`/incidentes/${incidente.id}/editar`} className="boton boton--secundario"><Icono nombre="editar" />Editar</Link>
            )}
            {puedeEliminar && (
              <button type="button" className="boton boton--peligro-suave" onClick={() => setConfirmando(true)}><Icono nombre="eliminar" />Eliminar</button>
            )}
          </div>
        )}
      </div>

      {/* Escritorio: contenido a la izquierda; estado, SLA y gestion a la derecha. En movil, una columna. */}
      <div className="detalle">
        <div className="detalle__principal">
          <section aria-labelledby="titulo-detalles" className="panel">
            <h2 id="titulo-detalles">Detalles</h2>
            <dl className="datos">
              <div><dt>Activo afectado</dt><dd><Link to={`/activos/${incidente.activoId}`}>{incidente.activo}</Link></dd></div>
              {puede(usuario, 'verOrganizaciones') && (
                <div><dt>Organización</dt><dd><Link to={`/organizaciones/${incidente.organizacionId}`}>Ver organización #{incidente.organizacionId}</Link></dd></div>
              )}
              <div><dt>Analista asignado</dt><dd>{incidente.analistaAsignado ?? 'Sin asignar'}</dd></div>
              <div><dt>Número</dt><dd>#{incidente.id}</dd></div>
              <div><dt>Creado</dt><dd>{formatearFecha(incidente.fechaCreacion)}</dd></div>
              <div><dt>Cerrado</dt><dd>{formatearFecha(incidente.fechaCierre, 'Aún abierto')}</dd></div>
            </dl>
            <h3>Descripción</h3>
            <p className="texto-largo">{incidente.descripcion}</p>
          </section>

          <BitacoraPanel incidenteId={incidente.id} puedeRegistrar={puedeSobreIncidente(usuario, 'registrarBitacora', incidente)} />

          <EvidenciasPanel incidenteId={incidente.id} puedeAgregar={puedeSobreIncidente(usuario, 'agregarEvidencia', incidente)} />
        </div>

        <div className="detalle__lateral">
          <SlaPanel incidenteId={incidente.id} />

          {(puedeSobreIncidente(usuario, 'cambiarEstado', incidente) || puede(usuario, 'asignarAnalista')) && (
            <section aria-labelledby="titulo-gestion" className="panel">
              <h2 id="titulo-gestion">Gestión</h2>
              <div className="gestion">
                {puedeSobreIncidente(usuario, 'cambiarEstado', incidente) && <CambiarEstadoForm incidente={incidente} />}
                {puede(usuario, 'asignarAnalista') && <AsignarAnalistaForm incidente={incidente} />}
              </div>
            </section>
          )}
        </div>
      </div>

      <ConfirmDialog
        abierto={confirmando}
        titulo="¿Eliminar este incidente?"
        mensaje={`Se eliminará “${incidente.titulo}” junto con su bitácora y evidencias. Esta acción no se puede deshacer.`}
        textoConfirmar="Sí, eliminar"
        procesando={eliminar.isPending}
        onConfirmar={confirmarEliminacion}
        onCancelar={() => setConfirmando(false)}
      />
    </>
  )
}

function CambiarEstadoForm({ incidente }: { incidente: Incidente }) {
  const estados = useEstados()
  const cambiar = useCambiarEstado(incidente.id)
  const { notificar } = useNotifications()

  // Estados a los que está permitido transicionar según el flujo de estados (ADR-008)
  const destinosPermitidos = useMemo(
    () => obtenerEstadosDestinoPermitidos(incidente.estadoId, estados.data ?? []),
    [incidente.estadoId, estados.data],
  )

  const [estadoId, setEstadoId] = useState(
    destinosPermitidos.length > 0 ? String(destinosPermitidos[0].id) : '',
  )

  useEffect(() => {
    if (destinosPermitidos.length > 0 && (!estadoId || !destinosPermitidos.some((d) => String(d.id) === estadoId))) {
      setEstadoId(String(destinosPermitidos[0].id))
    }
  }, [destinosPermitidos, estadoId])

  const enviar = (evento: FormEvent) => {
    evento.preventDefault()
    if (!estadoId) return
    cambiar.mutate(Number(estadoId), {
      onSuccess: () => notificar('Estado actualizado.'),
      onError: (error) => notificar(mensajeDeError(error), 'error'),
    })
  }

  if (destinosPermitidos.length === 0) {
    return (
      <p className="campo__ayuda">
        El incidente se encuentra en estado <strong>{incidente.estado}</strong> y no admite transiciones adicionales.
      </p>
    )
  }

  return (
    <form className="formulario-linea" onSubmit={enviar}>
      <div className="campo">
        <label htmlFor="nuevo-estado" className="campo__etiqueta">Estado</label>
        <select
          id="nuevo-estado"
          value={estadoId}
          onChange={(e) => setEstadoId(e.target.value)}
          disabled={estados.isPending || cambiar.isPending}
        >
          {destinosPermitidos.map((e) => (
            <option key={e.id} value={String(e.id)}>
              {e.nombre}
            </option>
          ))}
        </select>
      </div>
      <button type="submit" className="boton boton--primario" disabled={cambiar.isPending || !estadoId}>
        {cambiar.isPending ? 'Guardando…' : 'Cambiar estado'}
      </button>
    </form>
  )
}

function AsignarAnalistaForm({ incidente }: { incidente: Incidente }) {
  const analistas = useAnalistas(true)
  const asignar = useAsignarAnalista(incidente.id)
  const { notificar } = useNotifications()
  const [analistaId, setAnalistaId] = useState(incidente.analistaAsignadoId ? String(incidente.analistaAsignadoId) : '')

  const enviar = (evento: FormEvent) => {
    evento.preventDefault()
    asignar.mutate(Number(analistaId), {
      onSuccess: () => notificar('Analista asignado.'),
      onError: (error) => notificar(mensajeDeError(error), 'error'),
    })
  }

  return (
    <form className="formulario-linea" onSubmit={enviar}>
      <div className="campo">
        <label htmlFor="analista" className="campo__etiqueta">Analista</label>
        <select id="analista" value={analistaId} onChange={(e) => setAnalistaId(e.target.value)} disabled={analistas.isPending}>
          <option value="">Selecciona un analista</option>
          {analistas.data?.map((a) => <option key={a.id} value={String(a.id)}>{a.nombre}</option>)}
        </select>
      </div>
      <button type="submit" className="boton boton--primario" disabled={asignar.isPending || !analistaId}>
        {asignar.isPending ? 'Asignando…' : 'Asignar'}
      </button>
    </form>
  )
}
