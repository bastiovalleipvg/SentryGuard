import { useId, useState, type FormEvent } from 'react'

interface CampoBusqueda<C extends string> {
  nombre: C
  etiqueta: string
  tipo: 'busqueda'
}

interface CampoSeleccion<C extends string> {
  nombre: C
  etiqueta: string
  tipo: 'seleccion'
  opciones: { valor: string; texto: string }[]
  textoTodos?: string
}

export type CampoFiltro<C extends string> = CampoBusqueda<C> | CampoSeleccion<C>

interface Props<C extends string> {
  /** Nombre accesible del bloque de busqueda, por ejemplo "Filtrar activos". */
  etiqueta: string
  campos: CampoFiltro<C>[]
  valores: Record<C, string>
  onAplicar: (valores: Record<C, string>) => void
}

/** Formulario de filtros de los listados: se aplica al enviar (no en cada tecla) y se puede limpiar. */
export function FiltrosListado<C extends string>({ etiqueta, campos, valores, onAplicar }: Props<C>) {
  const [borrador, setBorrador] = useState(valores)
  const prefijo = useId()

  const aplicar = (evento: FormEvent) => {
    evento.preventDefault()
    onAplicar(borrador)
  }

  const limpiar = () => {
    const vacio = Object.fromEntries(campos.map((c) => [c.nombre, ''])) as Record<C, string>
    setBorrador(vacio)
    onAplicar(vacio)
  }

  return (
    <search className="filtros-contenedor" aria-label={etiqueta}>
      <form className={`filtros filtros--${campos.length}`} onSubmit={aplicar}>
        {campos.map((campo) => {
          const id = `${prefijo}-${campo.nombre}`
          return (
            <div className="campo" key={campo.nombre}>
              <label htmlFor={id} className="campo__etiqueta">{campo.etiqueta}</label>
              {campo.tipo === 'busqueda' ? (
                <input
                  id={id}
                  type="search"
                  maxLength={100}
                  value={borrador[campo.nombre]}
                  onChange={(e) => setBorrador((b) => ({ ...b, [campo.nombre]: e.target.value }))}
                />
              ) : (
                <select id={id} value={borrador[campo.nombre]} onChange={(e) => setBorrador((b) => ({ ...b, [campo.nombre]: e.target.value }))}>
                  <option value="">{campo.textoTodos ?? 'Todos'}</option>
                  {campo.opciones.map((o) => <option key={o.valor} value={o.valor}>{o.texto}</option>)}
                </select>
              )}
            </div>
          )
        })}
        <div className="acciones filtros__acciones">
          <button type="submit" className="boton boton--primario">Aplicar filtros</button>
          <button type="button" className="boton boton--secundario" onClick={limpiar}>Limpiar</button>
        </div>
      </form>
    </search>
  )
}
