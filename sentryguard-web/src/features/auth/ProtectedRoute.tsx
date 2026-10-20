import type { ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router'
import type { Rol } from '../../api/contracts'
import { LoadingState } from '../../components/LoadingState'
import { useAuth } from './useAuth'

interface Props {
  /** Si se indica, solo estos roles pueden entrar; el resto va a /acceso-denegado. */
  roles?: readonly Rol[]
  children?: ReactNode
}

export function ProtectedRoute({ roles, children }: Props) {
  const { estado, usuario } = useAuth()
  const location = useLocation()

  if (estado === 'verificando') {
    return <LoadingState mensaje="Verificando tu sesión…" />
  }

  if (estado !== 'autenticado' || !usuario) {
    return <Navigate to="/login" replace state={{ desde: location.pathname + location.search }} />
  }

  if (roles && !roles.includes(usuario.rol)) {
    return <Navigate to="/acceso-denegado" replace state={{ desde: location.pathname }} />
  }

  return children ?? <Outlet />
}
