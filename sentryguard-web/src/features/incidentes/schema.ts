import { z } from 'zod'

// Mismas reglas que valida la API (IncidenteAPI.ValidarTexto). La validacion del cliente es solo
// para dar respuesta inmediata: la del servidor es la que realmente protege.
export const LIMITES = {
  tituloMin: 5,
  tituloMax: 250,
  descripcionMin: 10,
  descripcionMax: 4000,
} as const

const idSeleccionado = (mensaje: string) => z.string().regex(/^\d+$/, mensaje)

export type ModoFormulario = 'crear' | 'editar'

export function incidenteFormSchema(modo: ModoFormulario) {
  return z.object({
    titulo: z
      .string()
      .trim()
      .min(LIMITES.tituloMin, `El título debe tener al menos ${LIMITES.tituloMin} caracteres.`)
      .max(LIMITES.tituloMax, `El título no puede superar ${LIMITES.tituloMax} caracteres.`),
    descripcion: z
      .string()
      .trim()
      .min(LIMITES.descripcionMin, `La descripción debe tener al menos ${LIMITES.descripcionMin} caracteres.`)
      .max(LIMITES.descripcionMax, `La descripción no puede superar ${LIMITES.descripcionMax} caracteres.`),
    severidadId: idSeleccionado('Selecciona la severidad.'),
    // El activo solo se elige al crear: un incidente no cambia de activo (ni de organizacion).
    activoId: modo === 'crear' ? idSeleccionado('Selecciona el activo afectado.') : z.string(),
  })
}

export type IncidenteFormValues = z.infer<ReturnType<typeof incidenteFormSchema>>

// Entrada manual de la bitacora (mismos limites que AgregarBitacora en la API).
export const bitacoraFormSchema = z.object({
  accion: z.string().trim().min(3, 'Describe la acción en al menos 3 caracteres.').max(100, 'La acción no puede superar 100 caracteres.'),
  comentario: z.string().trim().max(2000, 'El comentario no puede superar 2000 caracteres.'),
})
export type BitacoraFormValues = z.infer<typeof bitacoraFormSchema>

// Solo https:// o una ruta relativa simple: descarta javascript:, data:, file: y rutas absolutas del servidor.
// La misma regla la aplica la API (EsRutaEvidenciaValida); aqui solo adelanta el aviso.
export function esRutaEvidenciaValida(ruta: string): boolean {
  if (/^[a-z][a-z0-9+.-]*:/i.test(ruta)) {
    try {
      return new URL(ruta).protocol === 'https:'
    } catch {
      return false
    }
  }
  return !ruta.startsWith('/') && !ruta.includes('..') && /^[\p{L}\p{N}._\-/]+$/u.test(ruta)
}

export const evidenciaFormSchema = z.object({
  nombreArchivo: z.string().trim().min(1, 'Indica el nombre del archivo.').max(255, 'El nombre no puede superar 255 caracteres.'),
  rutaAlmacenamiento: z
    .string()
    .trim()
    .min(1, 'Indica dónde está guardada la evidencia.')
    .max(1000, 'La ubicación no puede superar 1000 caracteres.')
    .refine(esRutaEvidenciaValida, 'Usa una URL que empiece con https:// o una ruta relativa (letras, números, punto, guion, guion bajo y /).'),
})
export type EvidenciaFormValues = z.infer<typeof evidenciaFormSchema>
