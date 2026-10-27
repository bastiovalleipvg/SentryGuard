import { Link, useNavigate, useParams, useSearchParams } from 'react-router'
import { ConsultaRecurso } from '../../../components/ConsultaRecurso'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState } from '../../../components/ErrorState'
import { FiltrosListado } from '../../../components/FiltrosListado'
import { ListadoResponsivo } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { Pagination } from '../../../components/Pagination'
import { aEntero, useParametrosListado } from '../../../hooks/useParametrosListado'
import { NotFoundContent } from '../../../pages/NotFoundPage'
import { useAuth } from '../../auth/useAuth'
import { useRoles } from '../../catalogos/catalogosService'
import { UsuarioForm } from '../components/UsuarioForm'
import { useActualizarUsuario, useListaUsuarios, useRegistrarUsuario, useUsuario } from '../usuariosService'

export function UsuariosListPage() {
  const { filtros, valores, aplicar, cambiarPagina, hayFiltros, claveFormulario } = useParametrosListado(['q', 'rolId'] as const, ['rolId'])
  const consulta = useListaUsuarios(filtros)
  const roles = useRoles()
  const datos = consulta.data

  return (
    <>
      <div className="encabezado-pagina">
        <PageHeading>Usuarios</PageHeading>
        <Link to="/usuarios/nuevo" className="boton boton--primario"><span aria-hidden="true">+ </span>Nuevo usuario</Link>
      </div>
      <p className="introduccion">Cuentas de acceso a SentryGuard. Solo el Administrador puede crearlas y modificarlas.</p>

      <FiltrosListado
        key={claveFormulario}
        etiqueta="Filtrar usuarios"
        valores={valores}
        onAplicar={aplicar}
        campos={[
          { nombre: 'q', etiqueta: 'Buscar por nombre o correo', tipo: 'busqueda' },
          { nombre: 'rolId', etiqueta: 'Rol', tipo: 'seleccion', opciones: roles.data?.map((r) => ({ valor: String(r.id), texto: r.nombre })) ?? [] },
        ]}
      />

      <p className="resultado-busqueda" role="status" aria-live="polite">
        {datos && `${datos.total} ${datos.total === 1 ? 'usuario encontrado' : 'usuarios encontrados'}.`}
      </p>

      {consulta.isPending && <LoadingState mensaje="Cargando usuarios…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}
      {datos && datos.items.length === 0 && <EmptyState titulo={hayFiltros ? 'Ningún usuario coincide con los filtros' : 'Todavía no hay usuarios'} />}

      {datos && datos.items.length > 0 && (
        <div aria-busy={consulta.isFetching}>
          <ListadoResponsivo
            items={datos.items}
            etiqueta="Listado de usuarios"
            anchoTabla="64rem"
            clave={(u) => u.id}
            principal={{ encabezado: 'Nombre', render: (u) => <Link to={`/usuarios/${u.id}/editar`}>{u.nombre}</Link> }}
            columnas={[
              { encabezado: 'Correo', render: (u) => u.email },
              { encabezado: 'Rol', render: (u) => u.rol },
              { encabezado: 'Organización', render: (u) => u.organizacion ?? 'CyberOps (staff)' },
              {
                encabezado: 'Estado',
                render: (u) => <span className={`insignia ${u.activo ? 'insignia--resuelto' : 'insignia--cerrado'}`}>{u.activo ? 'Activo' : 'Desactivado'}</span>,
              },
            ]}
          />
          <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={cambiarPagina} etiqueta="Paginación de usuarios" />
        </div>
      )}
    </>
  )
}

export function UsuarioCreatePage() {
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const registrar = useRegistrarUsuario()
  const organizacionId = aEntero(useSearchParams()[0].get('organizacionId'))
  const roles = useRoles()
  const idCliente = roles.data?.find((r) => r.nombre === 'Cliente')?.id

  return (
    <>
      <PageHeading>Nuevo usuario</PageHeading>
      <p className="introduccion">Comunica la contraseña inicial por un canal seguro; la persona puede usarla de inmediato.</p>
      {/* Con ?organizacionId= se espera a tener el id del rol Cliente para preseleccionarlo. */}
      {organizacionId && roles.isPending ? (
        <LoadingState mensaje="Cargando formulario…" />
      ) : (
        <UsuarioForm
          modo="crear"
          textoEnviar="Crear usuario"
          urlCancelar={organizacionId ? `/organizaciones/${organizacionId}` : '/usuarios'}
          valoresIniciales={organizacionId && idCliente ? { rolId: String(idCliente), organizacionId: String(organizacionId) } : undefined}
          onEnviar={async (v, esCliente) => {
            await registrar.mutateAsync({
              nombre: v.nombre,
              email: v.email,
              password: v.password ?? '',
              rolId: Number(v.rolId),
              organizacionId: esCliente ? Number(v.organizacionId) : null,
            })
            notificar(`Usuario ${v.email} creado.`)
            navigate(organizacionId ? `/organizaciones/${organizacionId}` : '/usuarios')
          }}
        />
      )}
    </>
  )
}

export function UsuarioEditPage() {
  const id = Number(useParams().id)
  const { usuario: sesion } = useAuth()
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const consulta = useUsuario(id)
  const actualizar = useActualizarUsuario(id)

  if (!Number.isInteger(id) || id <= 0) return <NotFoundContent />
  return (
    <ConsultaRecurso consulta={consulta} titulo="Editar usuario" mensajeCarga="Cargando usuario…">
      {(u) => (
        <>
          <p className="miga"><Link to="/usuarios"><span aria-hidden="true">←</span>Volver a usuarios</Link></p>
          <PageHeading tituloDocumento={`Editar usuario ${u.nombre}`}>Editar usuario</PageHeading>
          <p className="introduccion">Correo: {u.email}. El correo no se modifica porque identifica la cuenta.</p>
          <UsuarioForm
            modo="editar"
            esCuentaPropia={sesion?.id === u.id}
            textoEnviar="Guardar cambios"
            urlCancelar="/usuarios"
            valoresIniciales={{
              nombre: u.nombre,
              email: u.email,
              rolId: String(u.rolId),
              organizacionId: u.organizacionId ? String(u.organizacionId) : '',
              activo: u.activo,
            }}
            onEnviar={async (v, esCliente) => {
              await actualizar.mutateAsync({
                nombre: v.nombre,
                rolId: Number(v.rolId),
                organizacionId: esCliente ? Number(v.organizacionId) : null,
                activo: v.activo,
              })
              notificar('Cambios guardados.')
              navigate('/usuarios')
            }}
          />
        </>
      )}
    </ConsultaRecurso>
  )
}
