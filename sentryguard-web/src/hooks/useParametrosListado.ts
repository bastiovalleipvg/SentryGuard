import { useSearchParams } from 'react-router'
import type { FiltrosListado } from '../api/contracts'

export function aEntero(valor: string | null | undefined): number | undefined {
  if (!valor || !/^\d+$/.test(valor)) return undefined
  return Number(valor)
}

/**
 * Pagina y filtros de un listado guardados en la URL (?page=2&q=...): se pueden compartir, recargar y
 * recorrer con Atras. Los parametros numericos se validan antes de enviarlos a la API.
 */
export function useParametrosListado<C extends string>(claves: readonly C[], numericas: readonly C[] = []) {
  const [params, setParams] = useSearchParams()

  const valores = Object.fromEntries(claves.map((c) => [c, params.get(c) ?? ''])) as Record<C, string>
  const filtros: FiltrosListado = { page: aEntero(params.get('page')) ?? 1 }
  for (const c of claves) {
    const valor = params.get(c)?.trim()
    if (!valor) continue
    filtros[c] = numericas.includes(c) ? aEntero(valor) : valor
  }

  const aplicar = (nuevos: Record<C, string>) => {
    const siguiente = new URLSearchParams()
    for (const c of claves) if (nuevos[c]?.trim()) siguiente.set(c, nuevos[c].trim())
    setParams(siguiente)
  }

  const cambiarPagina = (pagina: number) => {
    const siguiente = new URLSearchParams(params)
    siguiente.set('page', String(pagina))
    setParams(siguiente)
  }

  return {
    filtros,
    valores,
    aplicar,
    cambiarPagina,
    hayFiltros: claves.some((c) => Boolean(params.get(c))),
    /** Cambia cuando cambia la URL: sirve como key para reiniciar el formulario de filtros. */
    claveFormulario: params.toString(),
  }
}
