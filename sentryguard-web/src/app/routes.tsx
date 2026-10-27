import { lazy, Suspense, type ComponentType } from 'react'
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
import { LoadingState } from '../components/LoadingState'

// Vistas de los recursos relacionados: se cargan bajo demanda para no agrandar la carga inicial.
const diferida = (cargar: () => Promise<ComponentType>) => lazy(async () => ({ default: await cargar() }))
const ActivoDetailPage = diferida(() => import('../features/activos/pages/ActivoDetailPage').then((m) => m.ActivoDetailPage))
const ActivoCreatePage = diferida(() => import('../features/activos/pages/ActivoFormPages').then((m) => m.ActivoCreatePage))
const ActivoEditPage = diferida(() => import('../features/activos/pages/ActivoFormPages').then((m) => m.ActivoEditPage))
const ActivosListPage = diferida(() => import('../features/activos/pages/ActivosListPage').then((m) => m.ActivosListPage))
const AuditoriaPage = diferida(() => import('../features/auditoria/pages/AuditoriaPage').then((m) => m.AuditoriaPage))
const CatalogosPage = diferida(() => import('../features/catalogos/pages/CatalogosPage').then((m) => m.CatalogosPage))
const MiOrganizacionPage = diferida(() => import('../features/organizaciones/pages/OrganizacionDetailPage').then((m) => m.MiOrganizacionPage))
const OrganizacionDetailPage = diferida(() => import('../features/organizaciones/pages/OrganizacionDetailPage').then((m) => m.OrganizacionDetailPage))
const OrganizacionCreatePage = diferida(() => import('../features/organizaciones/pages/OrganizacionFormPages').then((m) => m.OrganizacionCreatePage))
const OrganizacionEditPage = diferida(() => import('../features/organizaciones/pages/OrganizacionFormPages').then((m) => m.OrganizacionEditPage))
const OrganizacionesListPage = diferida(() => import('../features/organizaciones/pages/OrganizacionesListPage').then((m) => m.OrganizacionesListPage))
const UsuarioCreatePage = diferida(() => import('../features/usuarios/pages/UsuariosPages').then((m) => m.UsuarioCreatePage))
const UsuarioEditPage = diferida(() => import('../features/usuarios/pages/UsuariosPages').then((m) => m.UsuarioEditPage))
const UsuariosListPage = diferida(() => import('../features/usuarios/pages/UsuariosPages').then((m) => m.UsuariosListPage))

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
      // Recursos relacionados. El rol se valida aqui solo para la interfaz: la API vuelve a validarlo.
      { path: 'activos', element: <Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><ActivosListPage /></Suspense> },
      { path: 'activos/nuevo', element: <ProtectedRoute roles={PERMISOS.crearActivo}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><ActivoCreatePage /></Suspense></ProtectedRoute> },
      { path: 'activos/:id', element: <Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><ActivoDetailPage /></Suspense> },
      { path: 'activos/:id/editar', element: <ProtectedRoute roles={PERMISOS.editarActivo}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><ActivoEditPage /></Suspense></ProtectedRoute> },
      { path: 'organizaciones', element: <ProtectedRoute roles={PERMISOS.verOrganizaciones}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><OrganizacionesListPage /></Suspense></ProtectedRoute> },
      {
        path: 'organizaciones/nueva',
        element: <ProtectedRoute roles={PERMISOS.gestionarOrganizaciones}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><OrganizacionCreatePage /></Suspense></ProtectedRoute>,
      },
      // Abierta a los 4 roles: un Cliente que pida una organizacion ajena recibe 403 de la API.
      { path: 'organizaciones/:id', element: <Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><OrganizacionDetailPage /></Suspense> },
      {
        path: 'organizaciones/:id/editar',
        element: <ProtectedRoute roles={PERMISOS.gestionarOrganizaciones}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><OrganizacionEditPage /></Suspense></ProtectedRoute>,
      },
      { path: 'mi-organizacion', element: <ProtectedRoute roles={['Cliente']}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><MiOrganizacionPage /></Suspense></ProtectedRoute> },
      { path: 'usuarios', element: <ProtectedRoute roles={PERMISOS.gestionarUsuarios}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><UsuariosListPage /></Suspense></ProtectedRoute> },
      { path: 'usuarios/nuevo', element: <ProtectedRoute roles={PERMISOS.gestionarUsuarios}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><UsuarioCreatePage /></Suspense></ProtectedRoute> },
      { path: 'usuarios/:id/editar', element: <ProtectedRoute roles={PERMISOS.gestionarUsuarios}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><UsuarioEditPage /></Suspense></ProtectedRoute> },
      { path: 'catalogos', element: <ProtectedRoute roles={PERMISOS.gestionarCatalogos}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><CatalogosPage /></Suspense></ProtectedRoute> },
      { path: 'auditoria', element: <ProtectedRoute roles={PERMISOS.verAuditoria}><Suspense fallback={<LoadingState mensaje="Cargando vista…" />}><AuditoriaPage /></Suspense></ProtectedRoute> },
      { path: 'acceso-denegado', element: <AccessDeniedPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
