import { Link } from 'react-router'
import type { Incidente } from '../../../api/contracts'
import { formatearFecha } from '../../../utils/formato'
import { EstadoBadge, SeveridadBadge } from './Badges'

export function IncidentesTable({ incidentes }: { incidentes: Incidente[] }) {
  return (
    <div className="tabla-contenedor">
      <table className="tabla">
        <caption className="visualmente-oculto">Listado de incidentes</caption>
        <thead>
          <tr>
            <th scope="col">Título</th>
            <th scope="col">Severidad</th>
            <th scope="col">Estado</th>
            <th scope="col">Activo</th>
            <th scope="col">Analista</th>
            <th scope="col">Creado</th>
          </tr>
        </thead>
        <tbody>
          {incidentes.map((i) => (
            <tr key={i.id}>
              <th scope="row">
                <Link to={`/incidentes/${i.id}`}>{i.titulo}</Link>
              </th>
              <td><SeveridadBadge severidad={i.severidad} /></td>
              <td><EstadoBadge estado={i.estado} /></td>
              <td>{i.activo}</td>
              <td>{i.analistaAsignado ?? 'Sin asignar'}</td>
              <td>{formatearFecha(i.fechaCreacion)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
