import { use } from 'react'
import { AuthContext, type AuthContextValue } from './AuthContext'

export function useAuth(): AuthContextValue {
  const contexto = use(AuthContext)
  if (!contexto) throw new Error('useAuth debe usarse dentro de <AuthProvider>.')
  return contexto
}
