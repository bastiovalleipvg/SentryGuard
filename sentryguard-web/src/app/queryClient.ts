import { QueryClient } from '@tanstack/react-query'
import { isApiError } from '../api/apiError'

export function crearQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        refetchOnWindowFocus: false,
        // Reintentar un 4xx no cambia el resultado (y un 429 empeoraria): solo se reintentan fallas de red/5xx.
        retry: (intentos, error) => {
          if (isApiError(error) && error.status >= 400 && error.status < 500) return false
          return intentos < 2
        },
      },
      mutations: { retry: false },
    },
  })
}
