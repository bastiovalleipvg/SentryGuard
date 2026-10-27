import { Link } from 'react-router'
import { useEstados, useSeveridades } from '../../catalogos/catalogosService'
import { useIncidentes } from '../queries'

interface Props {
  /** Parametros activos de la URL, para marcar la tarjeta que corresponde al filtro aplicado. */
  estadoId?: number
  severidadId?: number
  hayOtrosFiltros: boolean
}

/**
 * Conteos rapidos que funcionan como accesos directos a un filtro. Usan el mismo endpoint del listado
 * (la API ya limita cada conteo a lo que el rol puede ver) y se ocultan si la API falla, para no repetir
 * el mensaje de error del listado.
 */
export function ResumenIncidentes({ estadoId, severidadId, hayOtrosFiltros }: Props) {
  const estados = useEstados()
  const severidades = useSeveridades()
  const idAbierto = estados.data?.find((e) => e.nombre === 'Abierto')?.id
  const idProgreso = estados.data?.find((e) => e.nombre === 'En progreso')?.id
  const idCritica = severidades.data?.find((s) => s.nombre === 'Crítica')?.id

  const todos = useIncidentes({ page: 1 })
  const abiertos = useConteo({ estadoId: idAbierto })
  const enProgreso = useConteo({ estadoId: idProgreso })
  const criticos = useConteo({ severidadId: idCritica })

  const sinFiltro = !estadoId && !severidadId && !hayOtrosFiltros
  const tarjetas = [
    { texto: 'Todos', total: todos.data?.total, a: '/incidentes', activa: sinFiltro, tono: '' },
    { texto: 'Abiertos', total: abiertos.data?.total, a: `/incidentes?estadoId=${idAbierto}`, activa: !hayOtrosFiltros && !severidadId && estadoId === idAbierto, tono: 'resumen__item--abierto', listo: idAbierto },
    { texto: 'En progreso', total: enProgreso.data?.total, a: `/incidentes?estadoId=${idProgreso}`, activa: !hayOtrosFiltros && !severidadId && estadoId === idProgreso, tono: 'resumen__item--progreso', listo: idProgreso },
    { texto: 'Críticos', total: criticos.data?.total, a: `/incidentes?severidadId=${idCritica}`, activa: !hayOtrosFiltros && !estadoId && severidadId === idCritica, tono: 'resumen__item--critico', listo: idCritica },
  ]

  if (todos.isError) return null

  return (
    <ul className="resumen" aria-label="Resumen de incidentes">
      {tarjetas.map((t) =>
        'listo' in t && t.listo === undefined ? null : (
          <li key={t.texto}>
            <Link to={t.a} className={`resumen__item ${t.tono}`} aria-current={t.activa ? 'true' : undefined}>
              <span className="resumen__total">{t.total ?? '–'}</span>
              <span className="resumen__texto">{t.texto}</span>
            </Link>
          </li>
        ),
      )}
    </ul>
  )
}

/** Conteo de un filtro: solo se pide cuando ya se conoce el id del catalogo. */
function useConteo(filtro: { estadoId?: number; severidadId?: number }) {
  const habilitado = filtro.estadoId !== undefined || filtro.severidadId !== undefined
  return useIncidentes({ page: 1, ...filtro }, habilitado)
}
