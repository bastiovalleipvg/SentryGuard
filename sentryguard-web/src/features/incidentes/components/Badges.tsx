// Severidad y estado se comunican con texto y una forma (barras o punto), no solo con color
// (WCAG 1.4.1). Todos los pares texto/fondo superan 4.5:1 (ver variables en global.css).

const SEVERIDAD: Record<string, { clase: string; nivel: number }> = {
  Baja: { clase: 'insignia--baja', nivel: 1 },
  Media: { clase: 'insignia--media', nivel: 2 },
  Alta: { clase: 'insignia--alta', nivel: 3 },
  Crítica: { clase: 'insignia--critica', nivel: 4 },
}

/** Cuatro barras de altura creciente; se rellenan tantas como el nivel de severidad. */
function BarrasSeveridad({ nivel }: { nivel: number }) {
  return (
    <svg className="insignia__barras" viewBox="0 0 16 12" aria-hidden="true" focusable="false">
      {[0, 1, 2, 3].map((i) => (
        <rect key={i} x={i * 4} y={9 - i * 3} width="3" height={3 + i * 3} rx="0.75" fill="currentColor" opacity={i < nivel ? 1 : 0.25} />
      ))}
    </svg>
  )
}

export function SeveridadBadge({ severidad }: { severidad: string }) {
  const info = SEVERIDAD[severidad]
  return (
    <span className={`insignia ${info?.clase ?? ''}`}>
      <BarrasSeveridad nivel={info?.nivel ?? 0} />
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
    <span className={`insignia insignia--punto ${CLASE_ESTADO[estado] ?? ''}`}>
      <span className="visualmente-oculto">Estado: </span>
      {estado}
    </span>
  )
}
