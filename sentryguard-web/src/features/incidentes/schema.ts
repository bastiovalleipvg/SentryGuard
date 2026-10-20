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
