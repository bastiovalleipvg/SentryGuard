import { zodResolver } from '@hookform/resolvers/zod'
import type { UseQueryResult } from '@tanstack/react-query'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { ConfirmDialog } from '../../../components/ConfirmDialog'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState, mensajeDeError } from '../../../components/ErrorState'
import { FormField } from '../../../components/FormField'
import { ListadoResponsivo, type Columna } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { aplicarErroresApi, ResumenErrores } from '../../../components/ResumenErrores'
import { formatearHoras } from '../../../utils/formato'
import {
  useCategorias,
  useEstados,
  useMutacionesCatalogo,
  usePlanes,
  useSeveridades,
  type TipoCatalogo,
} from '../catalogosService'

interface Campo {
  nombre: string
  etiqueta: string
  tipo: 'texto' | 'horas' | 'textoLargo'
  obligatorio: boolean
  max: number
  ayuda?: string
}

interface ConfigCatalogo<T extends { id: number; nombre: string }> {
  tipo: TipoCatalogo
  titulo: string
  singular: string
  descripcion: string
  consulta: UseQueryResult<T[]>
  campos: Campo[]
  columnas: Columna<T>[]
  aValores: (item: T) => Record<string, string>
  /** Los estados no se editan ni eliminan: el flujo del incidente depende de sus nombres. */
  editable: boolean
}

const CAMPO_NOMBRE: Campo = { nombre: 'nombre', etiqueta: 'Nombre', tipo: 'texto', obligatorio: true, max: 50, ayuda: 'Entre 2 y 50 caracteres, sin repetir.' }
const horasValidas = (v: string) => /^\d+([.,]\d{1,2})?$/.test(v) && Number(v.replace(',', '.')) > 0 && Number(v.replace(',', '.')) <= 720

// Mismas reglas que CatalogoAPI en el servidor.
function esquemaDe(campos: Campo[]) {
  return z.object(
    Object.fromEntries(
      campos.map((c) => {
        let s = z.string().trim()
        if (c.tipo === 'horas') return [c.nombre, s.refine(horasValidas, 'Usa un número mayor que 0 y hasta 720, con hasta 2 decimales.')]
        if (c.obligatorio) s = s.min(2, `${c.etiqueta}: al menos 2 caracteres.`)
        return [c.nombre, s.max(c.max, `${c.etiqueta}: no puede superar ${c.max} caracteres.`)]
      }),
    ),
  )
}

function aCuerpo(campos: Campo[], valores: Record<string, string>): Record<string, unknown> {
  return Object.fromEntries(
    campos.map((c) => [c.nombre, c.tipo === 'horas' ? Number(valores[c.nombre].replace(',', '.')) : valores[c.nombre].trim() || null]),
  )
}

export function CatalogosPage() {
  const severidades = useSeveridades()
  const estados = useEstados()
  const categorias = useCategorias()
  const planes = usePlanes()

  return (
    <>
      <PageHeading>Catálogos</PageHeading>
      <p className="introduccion">
        Valores que usan los formularios y filtros del sistema. Un elemento en uso (por un incidente, un activo, una organización o un contrato) no se puede eliminar.
      </p>
      <nav aria-label="Catálogos de esta página" className="indice">
        <ul>
          <li><a href="#catalogo-severidades">Severidades</a></li>
          <li><a href="#catalogo-categorias-activo">Categorías de activo</a></li>
          <li><a href="#catalogo-planes-servicio">Planes de servicio</a></li>
          <li><a href="#catalogo-estados-incidente">Estados de incidente</a></li>
        </ul>
      </nav>

      <SeccionCatalogo
        config={{
          tipo: 'severidades',
          titulo: 'Severidades',
          singular: 'severidad',
          descripcion: 'Cada severidad define las horas de SLA por defecto (si la organización no tiene un contrato SLA que diga otra cosa).',
          consulta: severidades,
          campos: [CAMPO_NOMBRE, { nombre: 'horasSla', etiqueta: 'Horas de SLA', tipo: 'horas', obligatorio: true, max: 6, ayuda: 'Mayor que 0 y hasta 720.' }],
          columnas: [{ encabezado: 'Horas de SLA', render: (s) => formatearHoras(s.horasSLA) }],
          aValores: (s) => ({ nombre: s.nombre, horasSla: String(s.horasSLA) }),
          editable: true,
        }}
      />
      <SeccionCatalogo
        config={{
          tipo: 'categorias-activo',
          titulo: 'Categorías de activo',
          singular: 'categoría',
          descripcion: 'Clasificación de los activos (servidor, red, aplicación…).',
          consulta: categorias,
          campos: [CAMPO_NOMBRE],
          columnas: [],
          aValores: (c) => ({ nombre: c.nombre }),
          editable: true,
        }}
      />
      <SeccionCatalogo
        config={{
          tipo: 'planes-servicio',
          titulo: 'Planes de servicio',
          singular: 'plan',
          descripcion: 'Planes que contratan las organizaciones.',
          consulta: planes,
          campos: [
            CAMPO_NOMBRE,
            { nombre: 'descripcion', etiqueta: 'Descripción', tipo: 'textoLargo', obligatorio: false, max: 500, ayuda: 'Opcional. Hasta 500 caracteres.' },
            { nombre: 'tiempoRespuestaBaseH', etiqueta: 'Tiempo de respuesta base (horas)', tipo: 'horas', obligatorio: true, max: 6 },
          ],
          columnas: [
            { encabezado: 'Descripción', render: (p) => p.descripcion ?? 'Sin descripción' },
            { encabezado: 'Respuesta base', render: (p) => formatearHoras(p.tiempoRespuestaBaseH) },
          ],
          aValores: (p) => ({ nombre: p.nombre, descripcion: p.descripcion ?? '', tiempoRespuestaBaseH: String(p.tiempoRespuestaBaseH) }),
          editable: true,
        }}
      />
      <SeccionCatalogo
        config={{
          tipo: 'estados-incidente',
          titulo: 'Estados de incidente',
          singular: 'estado',
          descripcion: 'Los estados existentes no se editan ni eliminan porque el flujo del incidente depende de ellos (Abierto, Resuelto, Cerrado). Se pueden agregar estados nuevos.',
          consulta: estados,
          campos: [CAMPO_NOMBRE],
          columnas: [],
          aValores: (e) => ({ nombre: e.nombre }),
          editable: false,
        }}
      />
    </>
  )
}

function SeccionCatalogo<T extends { id: number; nombre: string }>({ config }: { config: ConfigCatalogo<T> }) {
  const { notificar } = useNotifications()
  const mutaciones = useMutacionesCatalogo(config.tipo)
  const [editando, setEditando] = useState<T | null>(null)
  const [eliminando, setEliminando] = useState<T | null>(null)
  const idSeccion = `catalogo-${config.tipo}`
  const datos = config.consulta.data

  const confirmarEliminacion = () => {
    if (!eliminando) return
    mutaciones.eliminar.mutate(eliminando.id, {
      onSuccess: () => notificar(`“${eliminando.nombre}” eliminado.`),
      onError: (error) => notificar(mensajeDeError(error), 'error'),
      onSettled: () => setEliminando(null),
    })
  }

  const columnas: Columna<T>[] = config.editable
    ? [
        ...config.columnas,
        {
          encabezado: 'Acciones',
          render: (item) => (
            <div className="acciones acciones--fila">
              <button type="button" className="boton boton--secundario" onClick={() => setEditando(item)}>
                Editar<span className="visualmente-oculto"> {item.nombre}</span>
              </button>
              <button type="button" className="boton boton--peligro" onClick={() => setEliminando(item)}>
                Eliminar<span className="visualmente-oculto"> {item.nombre}</span>
              </button>
            </div>
          ),
        },
      ]
    : config.columnas

  return (
    <section id={idSeccion} aria-labelledby={`${idSeccion}-titulo`} className="panel">
      <h2 id={`${idSeccion}-titulo`}>{config.titulo}</h2>
      <p className="campo__ayuda">{config.descripcion}</p>

      {config.consulta.isPending && <LoadingState mensaje={`Cargando ${config.titulo.toLowerCase()}…`} />}
      {config.consulta.isError && <ErrorState error={config.consulta.error} onReintentar={() => config.consulta.refetch()} />}
      {datos?.length === 0 && <EmptyState titulo="Sin elementos" />}
      {datos && datos.length > 0 && (
        <ListadoResponsivo
          items={datos}
          etiqueta={config.titulo}
          nivelTitulo={3}
          clave={(i) => i.id}
          principal={{ encabezado: 'Nombre', render: (i) => i.nombre }}
          columnas={columnas}
        />
      )}

      <FormularioCatalogo
        key={editando?.id ?? 'nuevo'}
        config={config}
        editando={editando}
        onTerminar={() => setEditando(null)}
      />

      <ConfirmDialog
        abierto={eliminando !== null}
        titulo={`¿Eliminar ${config.singular} “${eliminando?.nombre ?? ''}”?`}
        mensaje="Si está en uso, la API rechazará la eliminación y verás el motivo."
        textoConfirmar="Sí, eliminar"
        procesando={mutaciones.eliminar.isPending}
        onConfirmar={confirmarEliminacion}
        onCancelar={() => setEliminando(null)}
      />
    </section>
  )
}

interface PropsFormulario<T extends { id: number; nombre: string }> {
  config: ConfigCatalogo<T>
  editando: T | null
  onTerminar: () => void
}

function FormularioCatalogo<T extends { id: number; nombre: string }>({ config, editando, onTerminar }: PropsFormulario<T>) {
  const { notificar } = useNotifications()
  const mutaciones = useMutacionesCatalogo(config.tipo)
  const schema = useMemo(() => esquemaDe(config.campos), [config.campos])
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const tituloRef = useRef<HTMLHeadingElement>(null)
  const vacio = Object.fromEntries(config.campos.map((c) => [c.nombre, '']))

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<Record<string, string>>({
    resolver: zodResolver(schema),
    defaultValues: editando ? config.aValores(editando) : vacio,
  })

  // Al pulsar "Editar" el foco pasa al formulario, que es donde continua la tarea.
  useEffect(() => {
    if (editando) tituloRef.current?.focus()
  }, [editando])

  const enviar = handleSubmit(async (valores) => {
    setErrorGeneral(null)
    const cuerpo = aCuerpo(config.campos, valores)
    try {
      if (editando) {
        await mutaciones.actualizar.mutateAsync({ id: editando.id, datos: cuerpo })
        notificar('Cambios guardados.')
        onTerminar()
      } else {
        await mutaciones.crear.mutateAsync(cuerpo)
        notificar(`“${valores.nombre.trim()}” se agregó al catálogo.`)
        reset(vacio)
      }
    } catch (error) {
      setErrorGeneral(aplicarErroresApi(error, setError, config.campos.map((c) => c.nombre)))
    }
  })

  const idTitulo = `form-${config.tipo}-titulo`

  return (
    <form className="formulario formulario--compacto" onSubmit={enviar} noValidate aria-labelledby={idTitulo}>
      <h3 id={idTitulo} ref={tituloRef} tabIndex={-1}>
        {editando ? `Editar “${editando.nombre}”` : `Agregar ${config.singular}`}
      </h3>
      <ResumenErrores errorGeneral={errorGeneral} cantidadErrores={Object.keys(errors).length} enviado={submitCount > 0} />
      <div className="campos-en-linea">
        {config.campos.map((c) => (
          <FormField key={c.nombre} label={c.etiqueta} obligatorio={c.obligatorio} error={errors[c.nombre]?.message} ayuda={c.ayuda}>
            {c.tipo === 'textoLargo' ? (
              <textarea rows={2} maxLength={c.max} {...register(c.nombre)} />
            ) : (
              <input type="text" inputMode={c.tipo === 'horas' ? 'decimal' : undefined} autoComplete="off" maxLength={c.max} {...register(c.nombre)} />
            )}
          </FormField>
        ))}
      </div>
      <div className="acciones">
        {editando && (
          <button type="button" className="boton boton--secundario" onClick={onTerminar}>Cancelar edición</button>
        )}
        <button type="submit" className="boton boton--primario" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando…' : editando ? 'Guardar cambios' : 'Agregar'}
        </button>
      </div>
    </form>
  )
}
