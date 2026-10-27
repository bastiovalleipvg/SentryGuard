import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import type { ContratoSla } from '../../../api/contracts'
import { ConfirmDialog } from '../../../components/ConfirmDialog'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState, mensajeDeError } from '../../../components/ErrorState'
import { FormField } from '../../../components/FormField'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { formatearDia, formatearHoras } from '../../../utils/formato'
import { useSeveridades } from '../../catalogos/catalogosService'
import { useContratosSla, useMutacionesContrato } from '../organizacionesService'
import { contratoSlaFormSchema, type ContratoSlaFormValues } from '../schema'

interface Props {
  organizacionId: number
  puedeGestionar: boolean
}

type Accion = { tipo: 'finalizar' | 'eliminar'; contrato: ContratoSla }

// Contratos SLA de la organizacion (tb_ContratoSLA y tb_ContratoSLA_Detalle): horas pactadas por severidad.
// El SLA de cada incidente usa el contrato vigente al crearlo; sin contrato, las horas de la severidad.
export function ContratosSlaPanel({ organizacionId, puedeGestionar }: Props) {
  const consulta = useContratosSla(organizacionId)
  const mutaciones = useMutacionesContrato(organizacionId)
  const { notificar } = useNotifications()
  const [accion, setAccion] = useState<Accion | null>(null)

  const confirmar = () => {
    if (!accion) return
    const mutacion = accion.tipo === 'finalizar' ? mutaciones.finalizar : mutaciones.eliminar
    mutacion.mutate(accion.contrato.id, {
      onSuccess: () => notificar(accion.tipo === 'finalizar' ? 'Contrato finalizado.' : 'Contrato eliminado.'),
      onError: (error) => notificar(mensajeDeError(error), 'error'),
      onSettled: () => setAccion(null),
    })
  }

  return (
    <section aria-labelledby="titulo-contratos" className="panel">
      <h2 id="titulo-contratos">Contratos SLA</h2>
      {consulta.isPending && <LoadingState mensaje="Cargando contratos…" />}
      {consulta.isError && <ErrorState error={consulta.error} titulo="No se pudieron cargar los contratos" onReintentar={() => consulta.refetch()} />}
      {consulta.data?.length === 0 && (
        <EmptyState titulo="Sin contratos SLA">
          <p>Los incidentes de esta organización usan las horas definidas en cada severidad.</p>
        </EmptyState>
      )}
      {consulta.data && consulta.data.length > 0 && (
        <ul className="tarjetas tarjetas--simples" aria-label="Contratos SLA de la organización">
          {consulta.data.map((c) => (
            <li key={c.id}>
              <article className="tarjeta">
                <h3 className="tarjeta__titulo">
                  Contrato #{c.id}{' '}
                  <span className={`insignia ${c.vigenteHasta ? 'insignia--cerrado' : 'insignia--resuelto'}`}>
                    {c.vigenteHasta ? 'Finalizado o con término' : 'Vigente sin término'}
                  </span>
                </h3>
                <p>Desde {formatearDia(c.vigenteDesde)} hasta {formatearDia(c.vigenteHasta, 'sin fecha de término')}.</p>
                <dl className="tarjeta__datos">
                  {c.detalles.map((d) => (
                    <div key={d.severidadId}><dt>{d.severidad}</dt><dd>{formatearHoras(d.horasSla)}</dd></div>
                  ))}
                </dl>
                {puedeGestionar && (
                  <div className="acciones acciones--tarjeta">
                    {!c.vigenteHasta && (
                      <button type="button" className="boton boton--secundario" onClick={() => setAccion({ tipo: 'finalizar', contrato: c })}>
                        Finalizar<span className="visualmente-oculto"> contrato #{c.id}</span>
                      </button>
                    )}
                    <button type="button" className="boton boton--peligro" onClick={() => setAccion({ tipo: 'eliminar', contrato: c })}>
                      Eliminar<span className="visualmente-oculto"> contrato #{c.id}</span>
                    </button>
                  </div>
                )}
              </article>
            </li>
          ))}
        </ul>
      )}

      {puedeGestionar && <FormularioContrato organizacionId={organizacionId} />}

      <ConfirmDialog
        abierto={accion !== null}
        titulo={accion?.tipo === 'finalizar' ? '¿Finalizar este contrato?' : '¿Eliminar este contrato?'}
        mensaje={
          accion?.tipo === 'finalizar'
            ? 'El contrato quedará con fecha de término hoy. Los incidentes nuevos usarán otro contrato o las horas de la severidad.'
            : 'Se eliminará el contrato y sus horas por severidad. Esta acción no se puede deshacer.'
        }
        textoConfirmar={accion?.tipo === 'finalizar' ? 'Sí, finalizar' : 'Sí, eliminar'}
        procesando={mutaciones.finalizar.isPending || mutaciones.eliminar.isPending}
        onConfirmar={confirmar}
        onCancelar={() => setAccion(null)}
      />
    </section>
  )
}

function FormularioContrato({ organizacionId }: { organizacionId: number }) {
  const severidades = useSeveridades()
  const { crear } = useMutacionesContrato(organizacionId)
  const { notificar } = useNotifications()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  // Fecha local (toISOString daria el dia siguiente en Chile despues de las 21:00). 'sv-SE' formatea AAAA-MM-DD.
  const [hoy] = useState(() => new Date().toLocaleDateString('sv-SE'))

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<ContratoSlaFormValues>({
    resolver: zodResolver(contratoSlaFormSchema),
    defaultValues: { vigenteDesde: hoy, vigenteHasta: '', horas: {} },
  })

  const enviar = handleSubmit(async (v) => {
    setErrorGeneral(null)
    try {
      await crear.mutateAsync({
        vigenteDesde: v.vigenteDesde,
        vigenteHasta: v.vigenteHasta || null,
        detalles: Object.entries(v.horas)
          .filter(([, h]) => h.trim() !== '')
          // Las claves son "s<id>": una clave numerica haria que React Hook Form creara un arreglo.
          .map(([clave, h]) => ({ severidadId: Number(clave.slice(1)), horasSla: Number(h.replace(',', '.')) })),
      })
      reset({ vigenteDesde: hoy, vigenteHasta: '', horas: {} })
      notificar('Contrato SLA registrado.')
    } catch (error) {
      setErrorGeneral(aplicarErroresApi(error, setError, ['vigenteDesde', 'vigenteHasta']))
    }
  })

  // El refine sobre ['horas'] deja el error en errors.horas; en un record, .message se tiparia como una clave mas.
  const errorHoras = (errors.horas as { message?: string } | undefined)?.message

  return (
    <form className="formulario formulario--compacto" onSubmit={enviar} noValidate aria-labelledby="titulo-nuevo-contrato">
      <h3 id="titulo-nuevo-contrato">Registrar contrato SLA</h3>
      <ResumenErrores errorGeneral={errorGeneral} cantidadErrores={Object.keys(errors).length} enviado={submitCount > 0} />
      <div className="campos-en-linea">
        <FormField label="Vigente desde" obligatorio error={errors.vigenteDesde?.message}>
          <input type="date" {...register('vigenteDesde')} />
        </FormField>
        <FormField label="Vigente hasta" error={errors.vigenteHasta?.message} ayuda="Vacío si no tiene fecha de término.">
          <input type="date" {...register('vigenteHasta')} />
        </FormField>
      </div>

      <fieldset className="grupo-campos" aria-describedby={errorHoras ? 'error-horas-contrato' : 'ayuda-horas-contrato'}>
        <legend className="campo__etiqueta">Horas comprometidas por severidad</legend>
        <p id="ayuda-horas-contrato" className="campo__ayuda">Deja vacía una severidad para usar sus horas por defecto.</p>
        {severidades.isPending && <LoadingState mensaje="Cargando severidades…" />}
        <div className="campos-en-linea">
          {severidades.data?.map((s) => (
            <FormField key={s.id} label={s.nombre} error={errors.horas?.[`s${s.id}`]?.message} ayuda={`Por defecto: ${s.horasSLA} h`}>
              <input type="text" inputMode="decimal" autoComplete="off" {...register(`horas.s${s.id}`)} />
            </FormField>
          ))}
        </div>
        {errorHoras && (
          <p id="error-horas-contrato" className="campo__error"><span aria-hidden="true">⚠ </span>{errorHoras}</p>
        )}
      </fieldset>

      <div className="acciones">
        <button type="submit" className="boton boton--primario" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando…' : 'Registrar contrato'}
        </button>
      </div>
    </form>
  )
}
