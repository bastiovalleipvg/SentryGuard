import type { ReactNode } from 'react'
import { useMediaQuery } from '../hooks/useMediaQuery'

export interface Columna<T> {
  encabezado: string
  render: (item: T) => ReactNode
}

interface Props<T> {
  items: T[]
  /** Nombre del listado: caption de la tabla y aria-label de las tarjetas. */
  etiqueta: string
  clave: (item: T) => string | number
  /** Contenido de la primera columna (encabezado de fila) y del titulo de cada tarjeta, normalmente un enlace. */
  principal: Columna<T>
  columnas: Columna<T>[]
  /** Nivel del titulo de cada tarjeta: 2 en una pagina de listado, 3 dentro de una seccion con h2. */
  nivelTitulo?: 2 | 3
  /** Ancho desde el que se muestra la tabla; bajo ese ancho se usan tarjetas. */
  anchoTabla?: string
}

/**
 * Mismo listado en dos presentaciones: tabla con encabezados de fila en pantallas anchas y una tarjeta
 * por elemento en movil, sin scroll horizontal (WCAG 1.4.10).
 */
export function ListadoResponsivo<T>({ items, etiqueta, clave, principal, columnas, nivelTitulo = 2, anchoTabla = '48rem' }: Props<T>) {
  const esAncha = useMediaQuery(`(min-width: ${anchoTabla})`)
  const Titulo = nivelTitulo === 2 ? 'h2' : 'h3'

  if (esAncha) {
    return (
      <div className="tabla-contenedor">
        <table className="tabla">
          <caption className="visualmente-oculto">{etiqueta}</caption>
          <thead>
            <tr>
              <th scope="col">{principal.encabezado}</th>
              {columnas.map((c) => <th key={c.encabezado} scope="col">{c.encabezado}</th>)}
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={clave(item)}>
                <th scope="row">{principal.render(item)}</th>
                {columnas.map((c) => <td key={c.encabezado}>{c.render(item)}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    )
  }

  return (
    <ul className="tarjetas" aria-label={etiqueta}>
      {items.map((item) => (
        <li key={clave(item)}>
          <article className="tarjeta">
            <Titulo className="tarjeta__titulo">{principal.render(item)}</Titulo>
            <dl className="tarjeta__datos">
              {columnas.map((c) => (
                <div key={c.encabezado}>
                  <dt>{c.encabezado}</dt>
                  <dd>{c.render(item)}</dd>
                </div>
              ))}
            </dl>
          </article>
        </li>
      ))}
    </ul>
  )
}
