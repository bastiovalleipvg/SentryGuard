import { z } from 'zod'

/** "76.123.456-0" -> "76123456-0" (sin puntos, DV en mayuscula). Mismo criterio que Validacion.NormalizarRut. */
export function normalizarRut(valor: string): string {
  return valor.trim().replaceAll('.', '').toUpperCase()
}

export function rutTieneDvValido(rut: string): boolean {
  const [cuerpo, dv] = rut.split('-')
  let suma = 0
  let factor = 2
  for (let i = cuerpo.length - 1; i >= 0; i--) {
    suma += Number(cuerpo[i]) * factor
    factor = factor === 7 ? 2 : factor + 1
  }
  const resto = 11 - (suma % 11)
  const esperado = resto === 11 ? '0' : resto === 10 ? 'K' : String(resto)
  return dv === esperado
}

/**
 * El digito verificador solo se exige si el RUT cambia (igual que la API): asi se puede editar una
 * organizacion registrada antes de que existiera esta regla.
 */
export function organizacionFormSchema(rutOriginal?: string) {
  return z.object({
    razonSocial: z
      .string()
      .trim()
      .min(3, 'La razón social debe tener al menos 3 caracteres.')
      .max(200, 'La razón social no puede superar 200 caracteres.'),
    rut: z
      .string()
      .transform(normalizarRut)
      .refine((v) => /^\d{7,8}-[\dK]$/.test(v), 'Ingresa el RUT con guion, por ejemplo 76123456-0.')
      .refine((v) => v === rutOriginal || !/^\d{7,8}-[\dK]$/.test(v) || rutTieneDvValido(v), 'El dígito verificador del RUT no es válido.'),
    planId: z.string().regex(/^\d+$/, 'Selecciona el plan contratado.'),
  })
}

export type OrganizacionFormEntrada = z.input<ReturnType<typeof organizacionFormSchema>>
export type OrganizacionFormValues = z.output<ReturnType<typeof organizacionFormSchema>>

const fecha = z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Indica una fecha válida.')

// Contrato SLA: vigencia y horas comprometidas por severidad (vacio = la severidad no queda en el contrato).
export const contratoSlaFormSchema = z
  .object({
    vigenteDesde: fecha,
    vigenteHasta: z.union([z.literal(''), fecha]),
    horas: z.record(
      z.string(),
      z
        .string()
        .trim()
        .refine((v) => v === '' || (/^\d+([.,]\d{1,2})?$/.test(v) && Number(v.replace(',', '.')) > 0 && Number(v.replace(',', '.')) <= 720), 'Usa un número mayor que 0 y hasta 720, con hasta 2 decimales.'),
    ),
  })
  .refine((v) => !v.vigenteHasta || v.vigenteHasta > v.vigenteDesde, { path: ['vigenteHasta'], message: 'Debe ser posterior a la fecha de inicio.' })
  .refine((v) => Object.values(v.horas).some((h) => h.trim() !== ''), { path: ['horas'], message: 'Indica las horas de al menos una severidad.' })

export type ContratoSlaFormValues = z.infer<typeof contratoSlaFormSchema>
