import { useNavigate, useParams, useSearchParams } from 'react-router'
import { ConsultaRecurso } from '../../../components/ConsultaRecurso'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { aEntero } from '../../../hooks/useParametrosListado'
import { NotFoundContent } from '../../../pages/NotFoundPage'
import { useActivo, useActualizarActivo, useCrearActivo } from '../activosService'
import { ActivoForm } from '../components/ActivoForm'
import type { ActivoFormValues } from '../schema'

const aInput = (v: ActivoFormValues) => ({ nombre: v.nombre, hostnameIp: v.hostnameIp || null, categoriaId: Number(v.categoriaId) })

export function ActivoCreatePage() {
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const crear = useCrearActivo()
  // Desde el detalle de una organizacion se llega con ?organizacionId=N preseleccionado.
  const organizacionId = aEntero(useSearchParams()[0].get('organizacionId'))

  return (
    <>
      <PageHeading>Nuevo activo</PageHeading>
      <p className="introduccion">Los campos marcados como obligatorios deben completarse.</p>
      <ActivoForm
        modo="crear"
        textoEnviar="Registrar activo"
        urlCancelar={organizacionId ? `/organizaciones/${organizacionId}` : '/activos'}
        valoresIniciales={organizacionId ? { organizacionId: String(organizacionId) } : undefined}
        onEnviar={async (v) => {
          const activo = await crear.mutateAsync({ ...aInput(v), organizacionId: Number(v.organizacionId) })
          notificar('Activo registrado correctamente.')
          navigate(`/activos/${activo.id}`)
        }}
      />
    </>
  )
}

export function ActivoEditPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const { notificar } = useNotifications()
  const consulta = useActivo(id)
  const actualizar = useActualizarActivo(id)

  if (!Number.isInteger(id) || id <= 0) return <NotFoundContent />
  return (
    <ConsultaRecurso consulta={consulta} titulo="Editar activo" mensajeCarga="Cargando activo…">
      {(activo) => (
        <>
          <PageHeading tituloDocumento={`Editar activo ${activo.nombre}`}>Editar activo</PageHeading>
          <p className="introduccion">Organización: {activo.organizacion}. Un activo no cambia de organización.</p>
          <ActivoForm
            modo="editar"
            textoEnviar="Guardar cambios"
            urlCancelar={`/activos/${activo.id}`}
            valoresIniciales={{ nombre: activo.nombre, hostnameIp: activo.hostnameIp ?? '', categoriaId: String(activo.categoriaId) }}
            onEnviar={async (v) => {
              await actualizar.mutateAsync(aInput(v))
              notificar('Cambios guardados.')
              navigate(`/activos/${activo.id}`)
            }}
          />
        </>
      )}
    </ConsultaRecurso>
  )
}
