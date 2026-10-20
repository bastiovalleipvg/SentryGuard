import { useState, type FormEvent } from 'react'
import { useEstados, useSeveridades } from '../../catalogos/catalogosService'

export interface ValoresFiltro {
  q: string
  estadoId: string
  severidadId: string
}

interface Props {
  valores: ValoresFiltro
  onAplicar: (valores: ValoresFiltro) => void
}

export function IncidentesFilters({ valores, onAplicar }: Props) {
  const [borrador, setBorrador] = useState(valores)
  const estados = useEstados()
  const severidades = useSeveridades()

  const actualizar = (campo: keyof ValoresFiltro, valor: string) => setBorrador((b) => ({ ...b, [campo]: valor }))

  const aplicar = (evento: FormEvent) => {
    evento.preventDefault()
    onAplicar({ ...borrador, q: borrador.q.trim() })
  }

  const limpiar = () => {
    const vacio = { q: '', estadoId: '', severidadId: '' }
    setBorrador(vacio)
    onAplicar(vacio)
  }

  return (
    <search className="filtros-contenedor" aria-label="Filtrar incidentes">
      <form className="filtros" onSubmit={aplicar}>
        <div className="campo">
          <label htmlFor="filtro-q" className="campo__etiqueta">Buscar por título</label>
          <input id="filtro-q" type="search" maxLength={100} value={borrador.q} onChange={(e) => actualizar('q', e.target.value)} />
        </div>
        <div className="campo">
          <label htmlFor="filtro-estado" className="campo__etiqueta">Estado</label>
          <select id="filtro-estado" value={borrador.estadoId} onChange={(e) => actualizar('estadoId', e.target.value)}>
            <option value="">Todos</option>
            {estados.data?.map((e) => <option key={e.id} value={String(e.id)}>{e.nombre}</option>)}
          </select>
        </div>
        <div className="campo">
          <label htmlFor="filtro-severidad" className="campo__etiqueta">Severidad</label>
          <select id="filtro-severidad" value={borrador.severidadId} onChange={(e) => actualizar('severidadId', e.target.value)}>
            <option value="">Todas</option>
            {severidades.data?.map((s) => <option key={s.id} value={String(s.id)}>{s.nombre}</option>)}
          </select>
        </div>
        <div className="acciones filtros__acciones">
          <button type="submit" className="boton boton--primario">Aplicar filtros</button>
          <button type="button" className="boton boton--secundario" onClick={limpiar}>Limpiar</button>
        </div>
      </form>
    </search>
  )
}
