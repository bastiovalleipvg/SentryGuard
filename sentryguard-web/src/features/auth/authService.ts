import { LoginResponseSchema, UsuarioActualSchema, type LoginResponse, type UsuarioActual } from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

export const authService = {
  login(email: string, password: string): Promise<LoginResponse> {
    return apiRequest('/api/v1/usuarios/login', {
      method: 'POST',
      body: { email, password },
      schema: LoginResponseSchema,
      auth: false,
    })
  },

  obtenerUsuarioActual(): Promise<UsuarioActual> {
    return apiRequest('/api/v1/usuarios/me', { schema: UsuarioActualSchema })
  },
}
