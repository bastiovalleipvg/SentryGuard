/**
 * Estrategia de almacenamiento del JWT: sessionStorage (ver informe tecnico).
 *
 * - Se borra al cerrar la pestaña y no se comparte entre pestañas, a diferencia de localStorage.
 * - Sobrevive a recargar la pagina, a diferencia de guardarlo solo en memoria.
 * - Riesgo aceptado: un script inyectado (XSS) podria leerlo. Se mitiga con la CSP estricta del
 *   build, con React escapando todo el contenido (sin dangerouslySetInnerHTML), con la
 *   expiracion corta del JWT (60 min) y con el cierre de sesion que lo elimina.
 * - Una cookie HttpOnly evitaria la lectura por XSS, pero exigiria cambiar la API a
 *   autenticacion por cookies y agregar proteccion CSRF; queda fuera del alcance.
 *
 * El token nunca se escribe en la consola ni se pasa por la URL.
 */
const CLAVE = 'sentryguard.sesion'

interface SesionGuardada {
  token: string
  expiraEn: number
}

function leer(): SesionGuardada | null {
  try {
    const crudo = sessionStorage.getItem(CLAVE)
    if (!crudo) return null
    const valor = JSON.parse(crudo) as Partial<SesionGuardada>
    if (typeof valor.token !== 'string' || typeof valor.expiraEn !== 'number') return null
    return { token: valor.token, expiraEn: valor.expiraEn }
  } catch {
    return null
  }
}

export const tokenStorage = {
  guardar(token: string, expiraEnSegundos: number): void {
    const sesion: SesionGuardada = { token, expiraEn: Date.now() + expiraEnSegundos * 1000 }
    sessionStorage.setItem(CLAVE, JSON.stringify(sesion))
  },

  /** Devuelve el token solo si no ha expirado; si expiro, lo elimina. */
  obtenerToken(): string | null {
    const sesion = leer()
    if (!sesion) return null
    if (sesion.expiraEn <= Date.now()) {
      sessionStorage.removeItem(CLAVE)
      return null
    }
    return sesion.token
  },

  /** Milisegundos hasta que expire el token vigente, o null si no hay sesion. */
  msHastaExpirar(): number | null {
    const sesion = leer()
    return sesion ? sesion.expiraEn - Date.now() : null
  },

  limpiar(): void {
    sessionStorage.removeItem(CLAVE)
  },
}
