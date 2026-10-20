import { Link } from 'react-router'
import { PageHeading } from '../components/PageHeading'

export function AccessDeniedContent() {
  return (
    <div className="estado estado--denegado">
      <PageHeading>Acceso denegado</PageHeading>
      <p>Tu rol no tiene permiso para ver o realizar esta acción. Si crees que es un error, contacta a un administrador.</p>
      <p>
        <Link to="/incidentes" className="boton boton--secundario">Ir al listado de incidentes</Link>
      </p>
    </div>
  )
}

export function AccessDeniedPage() {
  return <AccessDeniedContent />
}
