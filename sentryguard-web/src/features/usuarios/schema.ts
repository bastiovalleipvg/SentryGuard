import { z } from 'zod'
import type { ModoFormulario } from '../incidentes/schema'

// Mismas reglas que UsuarioAPI / Validacion en el servidor.
export const REQUISITOS_PASSWORD = '8 a 128 caracteres, con al menos una mayúscula, una minúscula, un número y un símbolo.'

export function errorPassword(valor: string): string | null {
  if (valor.length < 8 || valor.length > 128) return 'Debe tener entre 8 y 128 caracteres.'
  if (!/\p{Lu}/u.test(valor) || !/\p{Ll}/u.test(valor) || !/\p{Nd}/u.test(valor) || !/[^\p{L}\p{Nd}]/u.test(valor)) {
    return 'Debe incluir mayúscula, minúscula, número y símbolo.'
  }
  return null
}

/**
 * Un Cliente pertenece a una organizacion y el staff de CyberOps no. El id del rol Cliente viene del
 * catalogo de roles (no se fija en el codigo).
 */
export function usuarioFormSchema(modo: ModoFormulario, idRolCliente: number | undefined) {
  return z
    .object({
      nombre: z.string().trim().min(3, 'El nombre debe tener al menos 3 caracteres.').max(150, 'El nombre no puede superar 150 caracteres.'),
      email: modo === 'crear'
        ? z.string().trim().max(320, 'El correo no puede superar 320 caracteres.').regex(/^[^@\s]+@[^@\s]+\.[^@\s]+$/, 'Ingresa un correo válido, por ejemplo nombre@empresa.cl.')
        : z.string(),
      password: modo === 'crear' ? z.string() : z.string().optional(),
      confirmacion: modo === 'crear' ? z.string() : z.string().optional(),
      rolId: z.string().regex(/^\d+$/, 'Selecciona el rol.'),
      organizacionId: z.string(),
      activo: z.boolean(),
    })
    .superRefine((v, ctx) => {
      if (modo === 'crear') {
        const error = errorPassword(v.password ?? '')
        if (error) ctx.addIssue({ code: 'custom', path: ['password'], message: error })
        else if (v.confirmacion !== v.password) ctx.addIssue({ code: 'custom', path: ['confirmacion'], message: 'Las contraseñas no coinciden.' })
      }
      if (idRolCliente !== undefined && Number(v.rolId) === idRolCliente && !/^\d+$/.test(v.organizacionId)) {
        ctx.addIssue({ code: 'custom', path: ['organizacionId'], message: 'Un usuario Cliente debe pertenecer a una organización.' })
      }
    })
}

export type UsuarioFormValues = z.infer<ReturnType<typeof usuarioFormSchema>>
