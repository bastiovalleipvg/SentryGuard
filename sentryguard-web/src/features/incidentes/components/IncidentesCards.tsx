import { Link } from 'react-router'
import type { Incidente } from '../../../api/contracts'
import { formatearFecha } from '../../../utils/formato'
import { EstadoBadge, SeveridadBadge } from './Badges'

interface Props {
  incidentes: Incidente[]
  orden?: string
  onCambiarOrden?: (campo: string) => void
}

const OPCIONES_ORDEN = [
  { valor: '', texto: 'Predeterminado (fecha descendente)' },
  { valor: 'fecha', texto: 'Fecha (más antiguo primero)' },
  { valor: '-fecha', texto: 'Fecha (más reciente primero)' },
  { valor: 'titulo', texto: 'Título (A - Z)' },
  { valor: '-titulo', texto: 'Título (Z - A)' },
  { valor: 'severidad', texto: 'Severidad (menor a mayor)' },
  { valor: '-severidad', texto: 'Severidad (mayor a menor)' },
  { valor: 'estado', texto: 'Estado (ascendente)' },
  { valor: '-estado', texto: 'Estado (descendente)' },
  { valor: 'categoria', texto: 'Categoría (A - Z)' },
  { valor: '-categoria', texto: 'Categoría (Z - A)' },
  { valor: 'activo', texto: 'Activo (A - Z)' },
  { valor: '-activo', texto: 'Activo (Z - A)' },
]

// Presentacion del listado para pantallas pequeñas (desde 320 px): una tarjeta por incidente.
export function IncidentesCards({ incidentes, orden = '', onCambiarOrden }: Props) {
  return (
    <div className="tarjetas-con-orden">
      {onCambiarOrden && (
        <div className="campo campo--movil-orden" style={{ marginBottom: '1rem' }}>
          <label htmlFor="ordenar-por-movil" className="campo__etiqueta">Ordenar por</label>
          <select
            id="ordenar-por-movil"
            value={orden}
            onChange={(e) => onCambiarOrden(e.target.value)}
          >
            {OPCIONES_ORDEN.map((op) => (
              <option key={op.valor} value={op.valor}>{op.texto}</option>
            ))}
          </select>
        </div>
      )}

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
                  <dt>Categoría</dt>
                  <dd>{i.categoria ?? 'Sin categoría'}</dd>
                </div>
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
    </div>
  )
}
