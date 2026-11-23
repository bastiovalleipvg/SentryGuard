import { useId, type ReactNode } from 'react'
import type { PuntoSerie } from '../api/contracts'

// Graficos SVG propios (sin librerias). Cada uno entrega: titulo, conclusion en texto, el grafico con
// rol "img" y una tabla con los mismos datos para quien no puede o no quiere leer el dibujo (WCAG 1.1.1, 1.4.1).

const nf = new Intl.NumberFormat('es-CL', { maximumFractionDigits: 2 })

interface MarcoProps {
  titulo: string
  /** Conclusion en una frase; se lee antes del grafico. */
  descripcion: string
  puntos: PuntoSerie[]
  unidad?: string
  children: ReactNode
}

function Marco({ titulo, descripcion, puntos, unidad = '', children }: MarcoProps) {
  const id = useId()
  return (
    <figure className="grafico" aria-labelledby={`${id}-t`} aria-describedby={`${id}-d`}>
      <h2 id={`${id}-t`} className="grafico__titulo">{titulo}</h2>
      <p id={`${id}-d`} className="grafico__descripcion">{descripcion}</p>
      {puntos.length === 0 ? <p className="meta">Sin datos para los filtros elegidos.</p> : children}
      {puntos.length > 0 && (
        <details className="grafico__datos">
          <summary>Ver los datos en tabla</summary>
          <table className="tabla">
            <caption className="visualmente-oculto">{titulo}</caption>
            <thead>
              <tr><th scope="col">Categoría</th><th scope="col">Valor</th></tr>
            </thead>
            <tbody>
              {puntos.map((p) => (
                <tr key={p.etiqueta}><th scope="row">{p.etiqueta}</th><td>{nf.format(p.valor)}{unidad}</td></tr>
              ))}
            </tbody>
          </table>
        </details>
      )}
    </figure>
  )
}

interface Base {
  titulo: string
  descripcion: string
  puntos: PuntoSerie[]
  unidad?: string
}

export function GraficoBarras({ titulo, descripcion, puntos, unidad = '' }: Base) {
  const ancho = 560
  const alto = 220
  const margen = { s: 20, i: 34, l: 8, d: 8 }
  const max = Math.max(...puntos.map((p) => p.valor), 1)
  const paso = (ancho - margen.l - margen.d) / Math.max(puntos.length, 1)
  const anchoBarra = Math.min(paso * 0.6, 56)
  return (
    <Marco titulo={titulo} descripcion={descripcion} puntos={puntos} unidad={unidad}>
      <svg viewBox={`0 0 ${ancho} ${alto}`} role="img" aria-label={`${titulo}. ${descripcion}`} className="grafico__svg">
        <line x1={margen.l} x2={ancho - margen.d} y1={alto - margen.i} y2={alto - margen.i} stroke="var(--borde-fuerte)" />
        {puntos.map((p, i) => {
          const h = ((alto - margen.i - margen.s) * p.valor) / max
          const x = margen.l + paso * i + (paso - anchoBarra) / 2
          const y = alto - margen.i - h
          return (
            <g key={p.etiqueta}>
              <rect x={x} y={y} width={anchoBarra} height={h} rx="3" fill="var(--primario)" />
              <text x={x + anchoBarra / 2} y={y - 5} textAnchor="middle" className="grafico__valor">{nf.format(p.valor)}{unidad}</text>
              <text x={x + anchoBarra / 2} y={alto - 14} textAnchor="middle" className="grafico__etiqueta">{p.etiqueta}</text>
            </g>
          )
        })}
      </svg>
    </Marco>
  )
}

export function GraficoLinea({ titulo, descripcion, puntos, unidad = '' }: Base) {
  const ancho = 560
  const alto = 220
  const margen = { s: 24, i: 34, l: 34, d: 16 }
  const max = Math.max(...puntos.map((p) => p.valor), 1)
  const paso = puntos.length > 1 ? (ancho - margen.l - margen.d) / (puntos.length - 1) : 0
  const coordenadas = puntos.map((p, i) => ({
    x: puntos.length > 1 ? margen.l + paso * i : (ancho + margen.l - margen.d) / 2,
    y: alto - margen.i - ((alto - margen.i - margen.s) * p.valor) / max,
    p,
  }))
  return (
    <Marco titulo={titulo} descripcion={descripcion} puntos={puntos} unidad={unidad}>
      <svg viewBox={`0 0 ${ancho} ${alto}`} role="img" aria-label={`${titulo}. ${descripcion}`} className="grafico__svg">
        <line x1={margen.l} x2={ancho - margen.d} y1={alto - margen.i} y2={alto - margen.i} stroke="var(--borde-fuerte)" />
        <polyline fill="none" stroke="var(--primario)" strokeWidth="2.5" points={coordenadas.map((c) => `${c.x},${c.y}`).join(' ')} />
        {coordenadas.map((c) => (
          <g key={c.p.etiqueta}>
            <circle cx={c.x} cy={c.y} r="4" fill="var(--primario)" />
            <text x={c.x} y={c.y - 9} textAnchor="middle" className="grafico__valor">{nf.format(c.p.valor)}{unidad}</text>
            <text x={c.x} y={alto - 14} textAnchor="middle" className="grafico__etiqueta">{c.p.etiqueta}</text>
          </g>
        ))}
      </svg>
    </Marco>
  )
}

export function GraficoBarrasHorizontales({ titulo, descripcion, puntos, unidad = '' }: Base) {
  const max = Math.max(...puntos.map((p) => p.valor), 1)
  return (
    <Marco titulo={titulo} descripcion={descripcion} puntos={puntos} unidad={unidad}>
      <ul className="barras-h" aria-hidden="true">
        {puntos.map((p) => (
          <li key={p.etiqueta} className="barras-h__fila">
            <span className="barras-h__etiqueta">{p.etiqueta}</span>
            <span className="barras-h__pista">
              <span className="barras-h__barra" style={{ width: `${(p.valor / max) * 100}%` }} />
            </span>
            <span className="barras-h__valor">{nf.format(p.valor)}{unidad}</span>
          </li>
        ))}
      </ul>
    </Marco>
  )
}
