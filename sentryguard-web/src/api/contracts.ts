import { z } from 'zod'

// Contrato de la API (DTOs de SentryGuard). Cada respuesta se valida con estos esquemas antes de
// llegar a la interfaz: si el servidor (o algo entre medio) devuelve algo inesperado, se rechaza
// en vez de renderizarlo (OWASP API10: consumo inseguro de APIs).

export const ROLES = ['Administrador', 'Supervisor', 'Analista', 'Cliente'] as const
export const RolSchema = z.enum(ROLES)
export type Rol = z.infer<typeof RolSchema>

export const LoginResponseSchema = z.object({
  token: z.string().min(1),
  expiresIn: z.number().int().positive(),
})
export type LoginResponse = z.infer<typeof LoginResponseSchema>

export const UsuarioActualSchema = z.object({
  id: z.number().int(),
  nombre: z.string(),
  email: z.string(),
  rol: RolSchema,
  organizacionId: z.number().int().nullable(),
})
export type UsuarioActual = z.infer<typeof UsuarioActualSchema>

export const IncidenteSchema = z.object({
  id: z.number().int(),
  titulo: z.string(),
  descripcion: z.string(),
  severidadId: z.number().int(),
  severidad: z.string(),
  estadoId: z.number().int(),
  estado: z.string(),
  activoId: z.number().int(),
  activo: z.string(),
  organizacionId: z.number().int(),
  analistaAsignadoId: z.number().int().nullable(),
  analistaAsignado: z.string().nullable(),
  fechaCreacion: z.string(),
  fechaCierre: z.string().nullable(),
})
export type Incidente = z.infer<typeof IncidenteSchema>

export const PaginaIncidentesSchema = z.object({
  total: z.number().int(),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalPages: z.number().int(),
  items: z.array(IncidenteSchema),
})
export type PaginaIncidentes = z.infer<typeof PaginaIncidentesSchema>

export const CreadoSchema = z.object({ id: z.number().int() })

const ItemCatalogoSchema = z.object({ id: z.number().int(), nombre: z.string() })
export type ItemCatalogo = z.infer<typeof ItemCatalogoSchema>
export const CatalogoSchema = z.object({ items: z.array(ItemCatalogoSchema) })

export const SeveridadSchema = ItemCatalogoSchema.extend({ horasSLA: z.number() })
export const CatalogoSeveridadesSchema = z.object({ items: z.array(SeveridadSchema) })
export type Severidad = z.infer<typeof SeveridadSchema>

export const ActivoSchema = z.object({
  id: z.number().int(),
  nombre: z.string(),
  hostnameIp: z.string().nullable(),
  categoria: z.string(),
})
export const PaginaActivosSchema = z.object({ items: z.array(ActivoSchema) })
export type Activo = z.infer<typeof ActivoSchema>

export const AnalistasSchema = z.array(ItemCatalogoSchema)

export interface FiltrosIncidentes {
  page: number
  q?: string
  estadoId?: number
  severidadId?: number
}

export interface CrearIncidenteInput {
  titulo: string
  descripcion: string
  activoId: number
  severidadId: number
}

export interface ActualizarIncidenteInput {
  titulo: string
  descripcion: string
  severidadId: number
}
