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
  categoriaId: z.number().int().nullish(),
  categoria: z.string().nullish(),
  fechaCreacion: z.string(),
  fechaCierre: z.string().nullable(),
  fechaModificacion: z.string().nullish(),
})
export type Incidente = z.infer<typeof IncidenteSchema>

/** Par (origen, destino) permitido del flujo de estados; viene de tb_TransicionEstado. */
export const TransicionSchema = z.object({ estadoOrigenId: z.number().int(), estadoDestinoId: z.number().int() })
export const ListaTransicionesSchema = z.array(TransicionSchema)
export type Transicion = z.infer<typeof TransicionSchema>

/** Respuesta paginada comun de la API: { total, page, pageSize, totalPages, items }. */
export function paginaDe<T extends z.ZodType>(item: T) {
  return z.object({
    total: z.number().int(),
    page: z.number().int(),
    pageSize: z.number().int(),
    totalPages: z.number().int(),
    items: z.array(item),
  })
}
export interface Pagina<T> {
  total: number
  page: number
  pageSize: number
  totalPages: number
  items: T[]
}

export const PaginaIncidentesSchema = paginaDe(IncidenteSchema)
export type PaginaIncidentes = z.infer<typeof PaginaIncidentesSchema>

export const CreadoSchema = z.object({ id: z.number().int() })

const ItemCatalogoSchema = z.object({ id: z.number().int(), nombre: z.string() })
export type ItemCatalogo = z.infer<typeof ItemCatalogoSchema>
export const CatalogoSchema = z.object({ items: z.array(ItemCatalogoSchema) })

export const SeveridadSchema = ItemCatalogoSchema.extend({ horasSLA: z.number() })
export const CatalogoSeveridadesSchema = z.object({ items: z.array(SeveridadSchema) })
export type Severidad = z.infer<typeof SeveridadSchema>

export const PlanServicioSchema = ItemCatalogoSchema.extend({
  descripcion: z.string().nullable(),
  tiempoRespuestaBaseH: z.number(),
})
export const CatalogoPlanesSchema = z.object({ items: z.array(PlanServicioSchema) })
export type PlanServicio = z.infer<typeof PlanServicioSchema>

export const ActivoSchema = z.object({
  id: z.number().int(),
  nombre: z.string(),
  hostnameIp: z.string().nullable(),
  categoriaId: z.number().int(),
  categoria: z.string(),
  organizacionId: z.number().int(),
  organizacion: z.string(),
})
export const PaginaActivosSchema = paginaDe(ActivoSchema)
export type Activo = z.infer<typeof ActivoSchema>

export const OrganizacionSchema = z.object({
  id: z.number().int(),
  razonSocial: z.string(),
  rut: z.string(),
  planId: z.number().int(),
  plan: z.string(),
  fechaAlta: z.string(),
})
export const PaginaOrganizacionesSchema = paginaDe(OrganizacionSchema)
export type Organizacion = z.infer<typeof OrganizacionSchema>

export const ContratoSlaSchema = z.object({
  id: z.number().int(),
  vigenteDesde: z.string(),
  vigenteHasta: z.string().nullable(),
  detalles: z.array(z.object({ severidadId: z.number().int(), severidad: z.string(), horasSla: z.number() })),
})
export const ListaContratosSlaSchema = z.object({ items: z.array(ContratoSlaSchema) })
export type ContratoSla = z.infer<typeof ContratoSlaSchema>

// Vista de administracion de usuarios: nunca incluye el hash de la contraseña (la API no lo envia).
export const UsuarioSchema = z.object({
  id: z.number().int(),
  nombre: z.string(),
  email: z.string(),
  rolId: z.number().int(),
  rol: RolSchema,
  organizacionId: z.number().int().nullable(),
  organizacion: z.string().nullable(),
  activo: z.boolean(),
})
export const PaginaUsuariosSchema = paginaDe(UsuarioSchema)
export type Usuario = z.infer<typeof UsuarioSchema>
export const UsuarioDetalleSchema = UsuarioSchema.omit({ organizacion: true })
export type UsuarioDetalle = z.infer<typeof UsuarioDetalleSchema>

export const AnalistasSchema = z.array(ItemCatalogoSchema)

export const BitacoraSchema = z.object({
  id: z.number().int(),
  accion: z.string(),
  comentario: z.string().nullable(),
  usuario: z.string(),
  timestamp: z.string(),
})
export const ListaBitacoraSchema = z.object({ items: z.array(BitacoraSchema) })
export type EntradaBitacora = z.infer<typeof BitacoraSchema>

export const EvidenciaSchema = z.object({
  id: z.number().int(),
  nombreArchivo: z.string(),
  rutaAlmacenamiento: z.string(),
  subidoPor: z.string(),
  fecha: z.string(),
})
export const ListaEvidenciasSchema = z.object({ items: z.array(EvidenciaSchema) })
export type Evidencia = z.infer<typeof EvidenciaSchema>

export const CumplimientoSlaSchema = z.object({
  horasTranscurridas: z.number(),
  horasSLA: z.number(),
  cumple: z.boolean(),
  fuente: z.enum(['Contrato SLA', 'Severidad']),
  fechaLimite: z.string(),
  cerrado: z.boolean(),
})
export type CumplimientoSla = z.infer<typeof CumplimientoSlaSchema>

export const AuditoriaSchema = z.object({
  id: z.number().int(),
  usuario: z.string().nullable(),
  accion: z.string(),
  entidad: z.string().nullish(),
  entidadId: z.number().int().nullish(),
  valoresAnteriores: z.string().nullish(),
  valoresNuevos: z.string().nullish(),
  ipOrigen: z.string().nullable(),
  timestamp: z.string(),
})
export const PaginaAuditoriaSchema = paginaDe(AuditoriaSchema)
export type RegistroAuditoria = z.infer<typeof AuditoriaSchema>

export interface FiltrosIncidentes {
  page: number
  q?: string
  estadoId?: number
  severidadId?: number
  activoId?: number
  categoriaId?: number
  orden?: string
}

/** Filtros de los listados simples: pagina mas parametros opcionales que van tal cual a la query. */
export interface FiltrosListado {
  page: number
  [parametro: string]: string | number | undefined
}

export interface CrearIncidenteInput {
  titulo: string
  descripcion: string
  activoId: number
  severidadId: number
  categoriaId?: number
}

export interface ActualizarIncidenteInput {
  titulo: string
  descripcion: string
  severidadId: number
  categoriaId?: number
}

// ==========================================
// Contratos para el Módulo de IA (RF-23 y RF-24)
// ==========================================

export const ClasificacionIaSchema = z.object({
  severidadSugeridaId: z.number().int(),
  severidadNombre: z.string(),
  categoriaSugerida: z.string(),
  justificacion: z.string(),
  accionesRecomendadas: z.array(z.string()),
  origen: z.string().optional(),
})
export type ClasificacionIa = z.infer<typeof ClasificacionIaSchema>

export const ResumenIaSchema = z.object({
  resumen: z.string(),
  generadoEn: z.string(),
  modelo: z.string(),
})
export type ResumenIa = z.infer<typeof ResumenIaSchema>

