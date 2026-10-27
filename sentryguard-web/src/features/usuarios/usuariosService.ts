import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AnalistasSchema,
  CreadoSchema,
  PaginaUsuariosSchema,
  UsuarioDetalleSchema,
  type FiltrosListado,
  type ItemCatalogo,
  type Pagina,
  type Usuario,
  type UsuarioDetalle,
} from '../../api/contracts'
import { apiRequest } from '../../api/httpClient'

const BASE = '/api/v1/usuarios'
export const TAMANO_PAGINA_USUARIOS = 10

export interface RegistroUsuarioInput {
  nombre: string
  email: string
  password: string
  rolId: number
  organizacionId: number | null
}

export interface ActualizarUsuarioInput {
  nombre: string
  rolId: number
  organizacionId: number | null
  activo: boolean
}

export const usuariosService = {
  analistas(signal?: AbortSignal): Promise<ItemCatalogo[]> {
    return apiRequest(`${BASE}/analistas`, { schema: AnalistasSchema, signal })
  },

  listar(filtros: FiltrosListado, signal?: AbortSignal): Promise<Pagina<Usuario>> {
    return apiRequest(BASE, { query: { pageSize: TAMANO_PAGINA_USUARIOS, ...filtros }, schema: PaginaUsuariosSchema, signal })
  },

  obtener(id: number, signal?: AbortSignal): Promise<UsuarioDetalle> {
    return apiRequest(`${BASE}/${id}`, { schema: UsuarioDetalleSchema, signal })
  },

  async registrar(input: RegistroUsuarioInput): Promise<number> {
    // La contraseña viaja solo en el cuerpo de esta peticion (HTTPS); nunca se guarda ni se registra en el cliente.
    const r = await apiRequest(`${BASE}/registro`, { method: 'POST', body: input, schema: CreadoSchema })
    return r.id
  },

  async actualizar(id: number, input: ActualizarUsuarioInput): Promise<void> {
    await apiRequest(`${BASE}/${id}`, { method: 'PUT', body: input })
  },
}

export const usuariosKeys = {
  todos: ['usuarios'] as const,
  analistas: ['usuarios', 'analistas'] as const,
  lista: (filtros: FiltrosListado) => ['usuarios', 'lista', filtros] as const,
  detalle: (id: number) => ['usuarios', 'detalle', id] as const,
}

export function useAnalistas(habilitado: boolean) {
  return useQuery({
    queryKey: usuariosKeys.analistas,
    queryFn: ({ signal }) => usuariosService.analistas(signal),
    enabled: habilitado,
  })
}

export function useListaUsuarios(filtros: FiltrosListado, habilitado = true) {
  return useQuery({
    queryKey: usuariosKeys.lista(filtros),
    queryFn: ({ signal }) => usuariosService.listar(filtros, signal),
    placeholderData: keepPreviousData,
    enabled: habilitado,
  })
}

export function useUsuario(id: number) {
  return useQuery({
    queryKey: usuariosKeys.detalle(id),
    queryFn: ({ signal }) => usuariosService.obtener(id, signal),
    enabled: Number.isInteger(id) && id > 0,
  })
}

export function useRegistrarUsuario() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: usuariosService.registrar, onSuccess: () => queryClient.invalidateQueries({ queryKey: usuariosKeys.todos }) })
}

export function useActualizarUsuario(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: ActualizarUsuarioInput) => usuariosService.actualizar(id, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usuariosKeys.todos }),
  })
}
