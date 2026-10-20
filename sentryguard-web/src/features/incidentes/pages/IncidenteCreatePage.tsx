import { useNavigate } from 'react-router'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { IncidenteForm } from '../components/IncidenteForm'
import { useCrearIncidente } from '../queries'

export function IncidenteCreatePage() {
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const crear = useCrearIncidente()

  return (
    <>
      <PageHeading>Nuevo incidente</PageHeading>
      <p className="introduccion">Los campos marcados como obligatorios deben completarse.</p>
      <IncidenteForm
        modo="crear"
        textoEnviar="Registrar incidente"
        urlCancelar="/incidentes"
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
