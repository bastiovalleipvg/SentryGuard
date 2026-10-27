import { useNavigate, useParams } from 'react-router'
import { ConsultaRecurso } from '../../../components/ConsultaRecurso'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { NotFoundContent } from '../../../pages/NotFoundPage'
import { OrganizacionForm } from '../components/OrganizacionForm'
import { useActualizarOrganizacion, useCrearOrganizacion, useOrganizacion } from '../organizacionesService'
import type { OrganizacionFormValues } from '../schema'

const aInput = (v: OrganizacionFormValues) => ({ razonSocial: v.razonSocial, rut: v.rut, planId: Number(v.planId) })

export function OrganizacionCreatePage() {
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const crear = useCrearOrganizacion()

  return (
    <>
      <PageHeading>Nueva organización</PageHeading>
      <p className="introduccion">Los campos marcados como obligatorios deben completarse.</p>
      <OrganizacionForm
        textoEnviar="Registrar organización"
        urlCancelar="/organizaciones"
        onEnviar={async (v) => {
          const org = await crear.mutateAsync(aInput(v))
          notificar('Organización registrada correctamente.')
          navigate(`/organizaciones/${org.id}`)
        }}
      />
    </>
  )
}

export function OrganizacionEditPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const consulta = useOrganizacion(id)
  const actualizar = useActualizarOrganizacion(id)

  if (!Number.isInteger(id) || id <= 0) return <NotFoundContent />
  return (
    <ConsultaRecurso consulta={consulta} titulo="Editar organización" mensajeCarga="Cargando organización…">
      {(org) => (
        <>
          <PageHeading tituloDocumento={`Editar ${org.razonSocial}`}>Editar organización</PageHeading>
          <OrganizacionForm
            textoEnviar="Guardar cambios"
            urlCancelar={`/organizaciones/${org.id}`}
            valoresIniciales={{ razonSocial: org.razonSocial, rut: org.rut, planId: String(org.planId) }}
            onEnviar={async (v) => {
              await actualizar.mutateAsync(aInput(v))
              notificar('Cambios guardados.')
              navigate(`/organizaciones/${org.id}`)
            }}
          />
        </>
      )}
    </ConsultaRecurso>
  )
}
