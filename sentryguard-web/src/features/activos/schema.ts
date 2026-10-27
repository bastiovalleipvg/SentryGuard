import { z } from 'zod'
import type { ModoFormulario } from '../incidentes/schema'

// Mismas reglas que ActivoAPI.ValidarAsync en el servidor.
export const LIMITES_ACTIVO = { nombreMin: 3, nombreMax: 150, hostMax: 255 } as const

const idSeleccionado = (mensaje: string) => z.string().regex(/^\d+$/, mensaje)

export function activoFormSchema(modo: ModoFormulario) {
  return z.object({
    nombre: z
      .string()
      .trim()
      .min(LIMITES_ACTIVO.nombreMin, `El nombre debe tener al menos ${LIMITES_ACTIVO.nombreMin} caracteres.`)
      .max(LIMITES_ACTIVO.nombreMax, `El nombre no puede superar ${LIMITES_ACTIVO.nombreMax} caracteres.`),
    hostnameIp: z
      .string()
      .trim()
      .max(LIMITES_ACTIVO.hostMax, `No puede superar ${LIMITES_ACTIVO.hostMax} caracteres.`)
      .refine((v) => v === '' || /^[A-Za-z0-9.:-]+$/.test(v), 'Usa un nombre de host o una IP (letras, números, punto, guion o dos puntos).'),
    categoriaId: idSeleccionado('Selecciona la categoría.'),
    // La organizacion solo se elige al crear: los incidentes del activo pertenecen a ella.
    organizacionId: modo === 'crear' ? idSeleccionado('Selecciona la organización dueña del activo.') : z.string(),
  })
}

export type ActivoFormValues = z.infer<ReturnType<typeof activoFormSchema>>
