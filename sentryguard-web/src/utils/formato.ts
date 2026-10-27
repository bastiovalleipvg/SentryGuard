const formatoFecha = new Intl.DateTimeFormat('es-CL', { dateStyle: 'medium', timeStyle: 'short' })

/** La API devuelve fechas UTC sin zona ("2026-10-20T14:03:00"); se interpretan como UTC. */
export function formatearFecha(valor: string | null, siVacia = 'Sin fecha'): string {
  if (!valor) return siVacia
  const conZona = /[zZ]|[+-]\d{2}:\d{2}$/.test(valor) ? valor : `${valor}Z`
  const fecha = new Date(conZona)
  return Number.isNaN(fecha.getTime()) ? siVacia : formatoFecha.format(fecha)
}

// Fechas sin hora (vigencia de contratos): se muestran en UTC para que no "retrocedan" un dia en Chile.
const formatoDia = new Intl.DateTimeFormat('es-CL', { dateStyle: 'medium', timeZone: 'UTC' })

export function formatearDia(valor: string | null, siVacia = 'Sin fecha'): string {
  if (!valor) return siVacia
  const fecha = new Date(/[zZ]|[+-]\d{2}:\d{2}$/.test(valor) ? valor : `${valor}Z`)
  return Number.isNaN(fecha.getTime()) ? siVacia : formatoDia.format(fecha)
}

const formatoHoras = new Intl.NumberFormat('es-CL', { maximumFractionDigits: 2 })

export function formatearHoras(horas: number): string {
  return `${formatoHoras.format(horas)} h`
}
