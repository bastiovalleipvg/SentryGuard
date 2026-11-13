import { useNavigate, useParams } from 'react-router'
import { isApiError } from '../../../api/apiError'
import { ErrorState } from '../../../components/ErrorState'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { AccessDeniedContent } from '../../../pages/AccessDeniedPage'
import { NotFoundContent } from '../../../pages/NotFoundPage'
import { puedeSobreIncidente } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { IncidenteForm } from '../components/IncidenteForm'
import { useActualizarIncidente, useIncidente } from '../queries'

export function IncidenteEditPage() {
  const id = Number(useParams().id)
  const { usuario } = useAuth()
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const consulta = useIncidente(id)
  const actualizar = useActualizarIncidente(id)

  if (!Number.isInteger(id) || id <= 0) return <NotFoundContent />
  if (consulta.isPending) return <LoadingState mensaje="Cargando incidente…" />
  if (consulta.isError) {
    if (isApiError(consulta.error) && consulta.error.status === 403) return <AccessDeniedContent />
    if (isApiError(consulta.error) && consulta.error.status === 404) return <NotFoundContent />
    return (
      <>
        <PageHeading>Editar incidente</PageHeading>
        <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />
      </>
    )
  }

  const incidente = consulta.data
  // Solo Administrador y Supervisor editan; si alguien mas llega por URL ve "acceso denegado" (la API igual responde 403).
  if (!puedeSobreIncidente(usuario, 'editarIncidente', incidente)) return <AccessDeniedContent />

  return (
    <>
      <PageHeading tituloDocumento={`Editar incidente #${incidente.id}`}>Editar incidente</PageHeading>
      <IncidenteForm
        modo="editar"
        textoEnviar="Guardar cambios"
        urlCancelar={`/incidentes/${incidente.id}`}
        valoresIniciales={{
          titulo: incidente.titulo,
          descripcion: incidente.descripcion,
          severidadId: String(incidente.severidadId),
          categoriaId: incidente.categoriaId ? String(incidente.categoriaId) : '',
        }}
        onEnviar={async (v) => {
          await actualizar.mutateAsync({ titulo: v.titulo, descripcion: v.descripcion, severidadId: Number(v.severidadId), categoriaId: v.categoriaId ? Number(v.categoriaId) : undefined })
          notificar('Cambios guardados.')
          navigate(`/incidentes/${incidente.id}`)
        }}
      />
    </>
  )
}
