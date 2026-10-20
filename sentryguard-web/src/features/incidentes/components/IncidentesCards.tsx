import { Link } from 'react-router'
import type { Incidente } from '../../../api/contracts'
import { formatearFecha } from '../../../utils/formato'
import { EstadoBadge, SeveridadBadge } from './Badges'

// Presentacion del listado para pantallas pequeñas (desde 320 px): una tarjeta por incidente.
export function IncidentesCards({ incidentes }: { incidentes: Incidente[] }) {
  return (
    <ul className="tarjetas" aria-label="Listado de incidentes">
      {incidentes.map((i) => (
        <li key={i.id}>
          <article className="tarjeta">
            <h2 className="tarjeta__titulo">
              <Link to={`/incidentes/${i.id}`}>{i.titulo}</Link>
            </h2>
            <p className="insignias">
              <SeveridadBadge severidad={i.severidad} />
              <EstadoBadge estado={i.estado} />
            </p>
            <dl className="tarjeta__datos">
              <div>
                <dt>Activo</dt>
                <dd>{i.activo}</dd>
              </div>
              <div>
                <dt>Analista</dt>
                <dd>{i.analistaAsignado ?? 'Sin asignar'}</dd>
              </div>
              <div>
                <dt>Creado</dt>
                <dd>{formatearFecha(i.fechaCreacion)}</dd>
              </div>
            </dl>
          </article>
        </li>
      ))}
    </ul>
  )
}
