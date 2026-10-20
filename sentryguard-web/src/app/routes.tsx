import { Navigate, type RouteObject } from 'react-router'
import { Layout } from '../components/Layout'
import { LoginPage } from '../features/auth/LoginPage'
import { PERMISOS } from '../features/auth/permisos'
import { ProtectedRoute } from '../features/auth/ProtectedRoute'
import { IncidenteCreatePage } from '../features/incidentes/pages/IncidenteCreatePage'
import { IncidenteDetailPage } from '../features/incidentes/pages/IncidenteDetailPage'
import { IncidenteEditPage } from '../features/incidentes/pages/IncidenteEditPage'
import { IncidentesListPage } from '../features/incidentes/pages/IncidentesListPage'
import { AccessDeniedPage } from '../pages/AccessDeniedPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { RouteErrorPage } from '../pages/RouteErrorPage'

export const routes: RouteObject[] = [
  { path: '/login', element: <LoginPage />, errorElement: <RouteErrorPage /> },
  {
    // Todo lo que esta bajo esta ruta exige sesion iniciada.
    element: (
      <ProtectedRoute>
        <Layout />
      </ProtectedRoute>
    ),
    errorElement: <RouteErrorPage />,
    children: [
      { index: true, element: <Navigate to="/incidentes" replace /> },
      { path: 'incidentes', element: <IncidentesListPage /> },
      {
        path: 'incidentes/nuevo',
        element: <ProtectedRoute roles={PERMISOS.crearIncidente}><IncidenteCreatePage /></ProtectedRoute>,
      },
      { path: 'incidentes/:id', element: <IncidenteDetailPage /> },
      {
        path: 'incidentes/:id/editar',
        element: <ProtectedRoute roles={PERMISOS.editarIncidente}><IncidenteEditPage /></ProtectedRoute>,
      },
      { path: 'acceso-denegado', element: <AccessDeniedPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
