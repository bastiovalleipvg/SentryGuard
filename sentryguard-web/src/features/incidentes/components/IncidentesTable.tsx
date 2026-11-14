import { Link } from 'react-router'
import type { Incidente } from '../../../api/contracts'
import { formatearFecha } from '../../../utils/formato'
import { EstadoBadge, SeveridadBadge } from './Badges'

interface Props {
  incidentes: Incidente[]
  orden?: string
  onCambiarOrden?: (campo: string) => void
}

function EncabezadoOrdenable({
  campo,
  etiqueta,
  ordenActual,
  onCambiarOrden,
}: {
  campo: string
  etiqueta: string
  ordenActual?: string
  onCambiarOrden?: (campo: string) => void
}) {
  const esAsc = ordenActual === campo
  const esDesc = ordenActual === `-${campo}`
  const ariaSort = esAsc ? 'ascending' : esDesc ? 'descending' : 'none'
  const icono = esAsc ? '▲' : esDesc ? '▼' : '↕'
  const textoOculto = esAsc
    ? `${etiqueta}, ordenado de forma ascendente. Activar para ordenar de forma descendente.`
    : esDesc
      ? `${etiqueta}, ordenado de forma descendente. Activar para quitar el orden.`
      : `${etiqueta}, sin orden. Activar para ordenar de forma ascendente.`

  const handleClick = () => {
    if (!onCambiarOrden) return
    if (esAsc) onCambiarOrden(`-${campo}`)
    else if (esDesc) onCambiarOrden('')
    else onCambiarOrden(campo)
  }

  return (
    <th scope="col" aria-sort={ariaSort}>
      {onCambiarOrden ? (
        <button type="button" className="boton-orden" onClick={handleClick}>
          <span>{etiqueta}</span>
          <span className="icono-orden" aria-hidden="true">{icono}</span>
          <span className="visualmente-oculto">{textoOculto}</span>
        </button>
      ) : (
        etiqueta
      )}
    </th>
  )
}

export function IncidentesTable({ incidentes, orden, onCambiarOrden }: Props) {
  return (
    <div className="tabla-contenedor">
      <table className="tabla">
        <caption className="visualmente-oculto">Listado de incidentes</caption>
        <thead>
          <tr>
            <EncabezadoOrdenable campo="titulo" etiqueta="Título" ordenActual={orden} onCambiarOrden={onCambiarOrden} />
            <EncabezadoOrdenable campo="severidad" etiqueta="Severidad" ordenActual={orden} onCambiarOrden={onCambiarOrden} />
            <EncabezadoOrdenable campo="estado" etiqueta="Estado" ordenActual={orden} onCambiarOrden={onCambiarOrden} />
            <EncabezadoOrdenable campo="categoria" etiqueta="Categoría" ordenActual={orden} onCambiarOrden={onCambiarOrden} />
            <EncabezadoOrdenable campo="activo" etiqueta="Activo" ordenActual={orden} onCambiarOrden={onCambiarOrden} />
            <th scope="col">Analista</th>
            <EncabezadoOrdenable campo="fecha" etiqueta="Creado" ordenActual={orden} onCambiarOrden={onCambiarOrden} />
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
              <td>{i.categoria ?? 'Sin categoría'}</td>
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
