import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { Providers } from '../app/Providers'
import { crearQueryClient } from '../app/queryClient'
import { routes } from '../app/routes'
import { tokenStorage } from '../features/auth/tokenStorage'
import { USUARIOS } from './fixtures'

interface Opciones {
  /** Usuario con sesion ya iniciada (token guardado como lo haria el login). */
  sesion?: keyof typeof USUARIOS
}

/** Renderiza la aplicacion completa (rutas, guards, providers) en una ruta inicial. */
export function renderApp(ruta: string, { sesion }: Opciones = {}) {
  if (sesion) tokenStorage.guardar(USUARIOS[sesion].token, 3600)
  const router = createMemoryRouter(routes, { initialEntries: [ruta] })
  const queryClient = crearQueryClient()
  queryClient.setDefaultOptions({ queries: { retry: false } })

  const user = userEvent.setup()
  const utils = render(
    <Providers queryClient={queryClient}>
      <RouterProvider router={router} />
    </Providers>,
  )
  return { ...utils, user, router }
}
