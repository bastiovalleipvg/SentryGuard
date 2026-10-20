const formatoFecha = new Intl.DateTimeFormat('es-CL', { dateStyle: 'medium', timeStyle: 'short' })

/** La API devuelve fechas UTC sin zona ("2026-10-20T14:03:00"); se interpretan como UTC. */
export function formatearFecha(valor: string | null): string {
  if (!valor) return '—'
  const conZona = /[zZ]|[+-]\d{2}:\d{2}$/.test(valor) ? valor : `${valor}Z`
  const fecha = new Date(conZona)
  return Number.isNaN(fecha.getTime()) ? '—' : formatoFecha.format(fecha)
}
