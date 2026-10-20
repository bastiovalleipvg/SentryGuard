import { useSyncExternalStore } from 'react'

export function useMediaQuery(consulta: string): boolean {
  return useSyncExternalStore(
    (alCambiar) => {
      const mql = window.matchMedia(consulta)
      mql.addEventListener('change', alCambiar)
      return () => mql.removeEventListener('change', alCambiar)
    },
    () => window.matchMedia(consulta).matches,
    () => false,
  )
}
