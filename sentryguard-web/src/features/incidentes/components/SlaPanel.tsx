import { ErrorState } from '../../../components/ErrorState'
import { LoadingState } from '../../../components/LoadingState'
import { formatearFecha, formatearHoras } from '../../../utils/formato'
import { useCumplimientoSla } from '../queries'

// Cumplimiento del SLA: horas pactadas (contrato de la organizacion o, si no hay, la severidad) contra
// el tiempo transcurrido desde la creacion hasta el cierre o hasta ahora.
export function SlaPanel({ incidenteId }: { incidenteId: number }) {
  const consulta = useCumplimientoSla(incidenteId)

  return (
    <section aria-labelledby="titulo-sla" className="panel">
      <h2 id="titulo-sla">Cumplimiento del SLA</h2>
      {consulta.isPending && <LoadingState mensaje="Calculando SLA…" />}
      {consulta.isError && <ErrorState error={consulta.error} titulo="No se pudo calcular el SLA" onReintentar={() => consulta.refetch()} />}
      {consulta.data && <DetalleSla sla={consulta.data} />}
    </section>
  )
}

function DetalleSla({ sla }: { sla: NonNullable<ReturnType<typeof useCumplimientoSla>['data']> }) {
  const porcentaje = sla.horasSLA > 0 ? Math.round((sla.horasTranscurridas / sla.horasSLA) * 100) : 0
  const estado = sla.cumple
    ? { clase: 'insignia--resuelto', icono: '✔', texto: sla.cerrado ? 'Cerrado dentro del plazo' : 'Dentro del plazo' }
    : { clase: 'insignia--critica', icono: '✖', texto: sla.cerrado ? 'Cerrado fuera de plazo' : 'Plazo vencido' }

  return (
    <>
      <p className="insignias">
        <span className={`insignia ${estado.clase}`}>
          <span aria-hidden="true">{estado.icono} </span>
          {estado.texto}
        </span>
      </p>
      {/* La barra es decorativa: el mismo dato va en texto en "Uso del plazo". */}
      <div className="barra-sla" aria-hidden="true">
        <span className={`barra-sla__relleno${sla.cumple ? '' : ' barra-sla__relleno--vencido'}`} style={{ width: `${Math.min(porcentaje, 100)}%` }} />
      </div>
      <dl className="datos">
        <div><dt>Plazo comprometido</dt><dd>{formatearHoras(sla.horasSLA)} (según {sla.fuente === 'Contrato SLA' ? 'el contrato SLA' : 'la severidad'})</dd></div>
        <div><dt>Tiempo {sla.cerrado ? 'hasta el cierre' : 'transcurrido'}</dt><dd>{formatearHoras(sla.horasTranscurridas)}</dd></div>
        <div><dt>Uso del plazo</dt><dd>{porcentaje} %</dd></div>
        <div><dt>Fecha límite</dt><dd>{formatearFecha(sla.fechaLimite)}</dd></div>
      </dl>
    </>
  )
}
