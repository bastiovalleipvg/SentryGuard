import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { Providers } from './app/Providers'
import { crearQueryClient } from './app/queryClient'
import { routes } from './app/routes'
import './styles/global.css'

const router = createBrowserRouter(routes)
const queryClient = crearQueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Providers queryClient={queryClient}>
      <RouterProvider router={router} />
    </Providers>
  </StrictMode>,
)
