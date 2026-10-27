import { useState } from 'react'
import { Icono } from '../../../components/Icono'
import { Link, Navigate, useNavigate, useParams } from 'react-router'
import type { Organizacion } from '../../../api/contracts'
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
import { formatearDia } from '../../../utils/formato'
import { useListaActivos } from '../../activos/activosService'
import { puede } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { useListaUsuarios } from '../../usuarios/usuariosService'
import { ContratosSlaPanel } from '../components/ContratosSlaPanel'
import { useEliminarOrganizacion, useOrganizacion } from '../organizacionesService'

export function OrganizacionDetailPage() {
  const id = Number(useParams().id)
  return <DetallePorId id={id} />
}

/** Un Cliente llega aqui desde "Mi organización": se usa la organizacion de su sesion, no un id de la URL. */
export function MiOrganizacionPage() {
  const { usuario } = useAuth()
  if (!usuario?.organizacionId) return <Navigate to="/incidentes" replace />
  return <DetallePorId id={usuario.organizacionId} />
}

function DetallePorId({ id }: { id: number }) {
  const consulta = useOrganizacion(id)
  if (!Number.isInteger(id) || id <= 0) return <NotFoundContent />
  return (
    <ConsultaRecurso consulta={consulta} titulo="Organización" mensajeCarga="Cargando organización…">
      {(org) => <DetalleOrganizacion organizacion={org} />}
    </ConsultaRecurso>
  )
}

function DetalleOrganizacion({ organizacion }: { organizacion: Organizacion }) {
  const { usuario } = useAuth()
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const [confirmando, setConfirmando] = useState(false)
  const eliminar = useEliminarOrganizacion()
  const esAdmin = puede(usuario, 'gestionarOrganizaciones')

  const confirmarEliminacion = () => {
    eliminar.mutate(organizacion.id, {
      onSuccess: () => {
        setConfirmando(false)
        notificar(`Organización “${organizacion.razonSocial}” eliminada.`)
        navigate('/organizaciones', { replace: true })
      },
      onError: (error) => {
        setConfirmando(false)
        notificar(mensajeDeError(error), 'error')
      },
    })
  }

  return (
    <>
      {puede(usuario, 'verOrganizaciones') && (
        <p className="miga"><Link to="/organizaciones"><Icono nombre="volver" />Volver a organizaciones</Link></p>
      )}

      <div className="encabezado-pagina">
        <PageHeading tituloDocumento={organizacion.razonSocial}>{organizacion.razonSocial}</PageHeading>
        {esAdmin && (
          <div className="acciones">
            <Link to={`/organizaciones/${organizacion.id}/editar`} className="boton boton--secundario"><Icono nombre="editar" />Editar</Link>
            <button type="button" className="boton boton--peligro-suave" onClick={() => setConfirmando(true)}><Icono nombre="eliminar" />Eliminar</button>
          </div>
        )}
      </div>

      <section aria-labelledby="titulo-datos-org" className="panel">
        <h2 id="titulo-datos-org">Datos de la organización</h2>
        <dl className="datos">
          <div><dt>RUT</dt><dd>{organizacion.rut}</dd></div>
          <div><dt>Plan contratado</dt><dd>{organizacion.plan}</dd></div>
          <div><dt>Cliente desde</dt><dd>{formatearDia(organizacion.fechaAlta)}</dd></div>
        </dl>
      </section>

      <ContratosSlaPanel organizacionId={organizacion.id} puedeGestionar={puede(usuario, 'gestionarContratosSla')} />

      <ActivosDeLaOrganizacion organizacionId={organizacion.id} puedeCrear={puede(usuario, 'crearActivo')} />

      {puede(usuario, 'gestionarUsuarios') && <UsuariosDeLaOrganizacion organizacionId={organizacion.id} />}

      <ConfirmDialog
        abierto={confirmando}
        titulo="¿Eliminar esta organización?"
        mensaje={`Se eliminará “${organizacion.razonSocial}”. Si tiene activos, incidentes, usuarios o contratos, la API rechazará la eliminación.`}
        textoConfirmar="Sí, eliminar"
        procesando={eliminar.isPending}
        onConfirmar={confirmarEliminacion}
        onCancelar={() => setConfirmando(false)}
      />
    </>
  )
}

function ActivosDeLaOrganizacion({ organizacionId, puedeCrear }: { organizacionId: number; puedeCrear: boolean }) {
  const [pagina, setPagina] = useState(1)
  const consulta = useListaActivos({ page: pagina, organizacionId })
  const datos = consulta.data

  return (
    <section aria-labelledby="titulo-activos-org" className="panel">
      <div className="encabezado-seccion">
        <h2 id="titulo-activos-org">Activos</h2>
        {puedeCrear && (
          <Link to={`/activos/nuevo?organizacionId=${organizacionId}`} className="boton boton--secundario">
            <Icono nombre="mas" />Nuevo activo
          </Link>
        )}
      </div>
      {consulta.isPending && <LoadingState mensaje="Cargando activos…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}
      {datos?.items.length === 0 && <EmptyState compacto titulo="Esta organización no tiene activos registrados" />}
      {datos && datos.items.length > 0 && (
        <>
          <ListadoResponsivo
            items={datos.items}
            etiqueta="Activos de la organización"
            nivelTitulo={3}
            clave={(a) => a.id}
            principal={{ encabezado: 'Nombre', render: (a) => <Link to={`/activos/${a.id}`}>{a.nombre}</Link> }}
            columnas={[
              { encabezado: 'Hostname o IP', render: (a) => a.hostnameIp ?? 'Sin dato' },
              { encabezado: 'Categoría', render: (a) => a.categoria },
            ]}
          />
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={setPagina} etiqueta="Paginación de activos de la organización" />
        </>
      )}
    </section>
  )
}

function UsuariosDeLaOrganizacion({ organizacionId }: { organizacionId: number }) {
  const [pagina, setPagina] = useState(1)
  const consulta = useListaUsuarios({ page: pagina, organizacionId })
  const datos = consulta.data

  return (
    <section aria-labelledby="titulo-usuarios-org" className="panel">
      <div className="encabezado-seccion">
        <h2 id="titulo-usuarios-org">Usuarios de la organización</h2>
        <Link to={`/usuarios/nuevo?organizacionId=${organizacionId}`} className="boton boton--secundario">
          <Icono nombre="mas" />Nuevo usuario
        </Link>
      </div>
      {consulta.isPending && <LoadingState mensaje="Cargando usuarios…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}
      {datos?.items.length === 0 && <EmptyState compacto titulo="Esta organización no tiene usuarios" />}
      {datos && datos.items.length > 0 && (
        <>
          <ListadoResponsivo
            items={datos.items}
            etiqueta="Usuarios de la organización"
            nivelTitulo={3}
            clave={(u) => u.id}
            principal={{ encabezado: 'Nombre', render: (u) => <Link to={`/usuarios/${u.id}/editar`}>{u.nombre}</Link> }}
            columnas={[
              { encabezado: 'Correo', render: (u) => u.email },
              { encabezado: 'Estado', render: (u) => (u.activo ? 'Activo' : 'Desactivado') },
            ]}
          />
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={setPagina} etiqueta="Paginación de usuarios de la organización" />
        </>
      )}
    </section>
  )
}
