import { Link } from 'react-router'
import { PageHeading } from '../components/PageHeading'

export function NotFoundContent() {
  return (
    <div className="estado">
      <PageHeading>No encontrado</PageHeading>
      <p>La página o el incidente que buscas no existe, o fue eliminado.</p>
      <p>
        <Link to="/incidentes" className="boton boton--secundario">Ir al listado de incidentes</Link>
      </p>
    </div>
  )
}

export function NotFoundPage() {
  return <NotFoundContent />
}
