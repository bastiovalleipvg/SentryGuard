// Severidad y estado se comunican con texto e icono, no solo con color (WCAG 1.4.1).
// Todos los pares texto/fondo superan 4.5:1 (ver variables en global.css).

const CLASE_SEVERIDAD: Record<string, string> = {
  Baja: 'insignia--baja',
  Media: 'insignia--media',
  Alta: 'insignia--alta',
  Crítica: 'insignia--critica',
}

const ICONO_SEVERIDAD: Record<string, string> = {
  Baja: '▁',
  Media: '▃',
  Alta: '▅',
  Crítica: '▇',
}

export function SeveridadBadge({ severidad }: { severidad: string }) {
  return (
    <span className={`insignia ${CLASE_SEVERIDAD[severidad] ?? ''}`}>
      <span aria-hidden="true">{ICONO_SEVERIDAD[severidad] ?? '•'} </span>
      <span className="visualmente-oculto">Severidad: </span>
      {severidad}
    </span>
  )
}

const CLASE_ESTADO: Record<string, string> = {
  Abierto: 'insignia--abierto',
  'En progreso': 'insignia--progreso',
  Escalado: 'insignia--escalado',
  Resuelto: 'insignia--resuelto',
  Cerrado: 'insignia--cerrado',
}

export function EstadoBadge({ estado }: { estado: string }) {
  return (
    <span className={`insignia ${CLASE_ESTADO[estado] ?? ''}`}>
      <span className="visualmente-oculto">Estado: </span>
      {estado}
    </span>
  )
}
