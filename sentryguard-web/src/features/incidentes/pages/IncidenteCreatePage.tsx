import { useNavigate, useSearchParams } from 'react-router'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { aEntero } from '../../../hooks/useParametrosListado'
import { IncidenteForm } from '../components/IncidenteForm'
import { useCrearIncidente } from '../queries'

export function IncidenteCreatePage() {
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const crear = useCrearIncidente()
  // Desde el detalle de un activo se llega con ?activoId=N preseleccionado.
  const activoId = aEntero(useSearchParams()[0].get('activoId'))

  return (
    <>
      <PageHeading>Nuevo incidente</PageHeading>
      <p className="introduccion">Los campos marcados como obligatorios deben completarse.</p>
      <IncidenteForm
        modo="crear"
        textoEnviar="Registrar incidente"
        urlCancelar={activoId ? `/activos/${activoId}` : '/incidentes'}
        valoresIniciales={activoId ? { activoId: String(activoId) } : undefined}
        onEnviar={async (v) => {
          const id = await crear.mutateAsync({
            titulo: v.titulo,
            descripcion: v.descripcion,
            activoId: Number(v.activoId),
            severidadId: Number(v.severidadId),
          })
          notificar('Incidente registrado correctamente.')
          navigate(`/incidentes/${id}`)
        }}
      />
    </>
  )
}
