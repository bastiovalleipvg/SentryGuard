import { useState } from 'react'
import type { Indicador, Indicadores, PuntoSerie, SeriesDashboard } from '../../../api/contracts'
import { isApiError } from '../../../api/apiError'
import { ErrorState, mensajeDeError } from '../../../components/ErrorState'
import { FiltrosListado, type CampoFiltro } from '../../../components/FiltrosListado'
import { GraficoBarras, GraficoBarrasHorizontales, GraficoLinea } from '../../../components/Graficos'
import { LoadingState } from '../../../components/LoadingState'
import { PageHeading } from '../../../components/PageHeading'
import { useParametrosListado } from '../../../hooks/useParametrosListado'
import { useCategoriasIncidente, useSeveridades } from '../../catalogos/catalogosService'
import { useResumirIa } from '../../ia/iaService'
import { puede } from '../../auth/permisos'
import { useAuth } from '../../auth/useAuth'
import { useOpcionesOrganizaciones } from '../../organizaciones/organizacionesService'
import { useIndicadores, useSeries } from '../dashboardService'

const nf = new Intl.NumberFormat('es-CL', { maximumFractionDigits: 2 })

type Clave = 'desde' | 'hasta' | 'organizacionId' | 'severidadId' | 'categoriaId'

function textoValor(i: Indicador): string {
  if (i.valor === null) return 'Sin datos'
  return `${nf.format(i.valor)}${i.unidad === 'porcentaje' ? ' %' : i.unidad === 'horas' ? ' h' : ''}`
}

/** Variacion respecto del periodo anterior de igual duracion; el significado va en texto, no solo en color. */
function textoVariacion(i: Indicador): string {
  if (i.variacion === null) return 'Sin período anterior con que comparar'
  if (i.variacion === 0) return 'Sin cambio respecto del período anterior'
  const unidad = i.unidad === 'porcentaje' ? ' pp' : i.unidad === 'horas' ? ' h' : ''
  return `${i.variacion > 0 ? '▲ +' : '▼ '}${nf.format(i.variacion)}${unidad} respecto del período anterior`
}

function mayor(puntos: PuntoSerie[]): PuntoSerie | undefined {
  return puntos.reduce<PuntoSerie | undefined>((a, p) => (!a || p.valor > a.valor ? p : a), undefined)
}

function conclusion(puntos: PuntoSerie[], que: string): string {
  const m = mayor(puntos)
  return m ? `${que}: ${m.etiqueta} (${nf.format(m.valor)}).` : 'Sin datos en el período elegido.'
}

/** Resumen numerico agregado que se envia a la IA: solo cifras, nunca datos de personas. */
function contextoParaIa(indicadores: Indicadores, series: SeriesDashboard): string {
  const lineas = [`Período ${indicadores.desde} a ${indicadores.hasta}.`]
  for (const i of indicadores.items) lineas.push(`${i.nombre}: ${textoValor(i)} (${textoVariacion(i)}).`)
  const lista = (titulo: string, ps: PuntoSerie[]) => {
    if (ps.length > 0) lineas.push(`${titulo}: ${ps.map((p) => `${p.etiqueta}=${nf.format(p.valor)}`).join(', ')}.`)
  }
  lista('Incidentes por severidad', series.porSeveridad)
  lista('Incidentes por categoría', series.porCategoria)
  lista('Incidentes por mes', series.incidentesPorMes)
  return lineas.join(' ')
}

function ResumenIa({ indicadores, series }: { indicadores: Indicadores; series: SeriesDashboard }) {
  const ia = useResumirIa()
  const [error, setError] = useState<string | null>(null)

  const generar = () => {
    setError(null)
    ia.mutate({ contexto: contextoParaIa(indicadores, series), tipo: 'dashboard' }, { onError: (e) => setError(mensajeDeError(e)) })
  }

  return (
    <section className="resumen-ia" aria-labelledby="titulo-resumen-ia">
      <h2 id="titulo-resumen-ia" className="grafico__titulo">Resumen del período</h2>
      <p className="meta">La IA recibe solo las cifras agregadas de este dashboard. Revisa el texto antes de usarlo.</p>
      <button type="button" className="boton boton--secundario" onClick={generar} disabled={ia.isPending}>
        {ia.isPending ? 'Generando resumen…' : 'Resumir con IA'}
      </button>
      <div role="status" aria-live="polite" className="resumen-ia__resultado">
        {error && <p className="alerta alerta--error">{error}</p>}
        {ia.data && !error && (
          <>
            <p className="etiqueta-ia">Generado con IA</p>
            <p>{ia.data.resumen}</p>
          </>
        )}
      </div>
    </section>
  )
}

export function DashboardPage() {
  const { usuario } = useAuth()
  const esStaff = usuario?.rol === 'Administrador' || usuario?.rol === 'Supervisor'
  const { filtros, valores, aplicar, claveFormulario } = useParametrosListado<Clave>(
    ['desde', 'hasta', 'organizacionId', 'severidadId', 'categoriaId'],
    ['organizacionId', 'severidadId', 'categoriaId'],
  )
  const { page: _pagina, ...filtrosApi } = filtros
  void _pagina

  const indicadores = useIndicadores(filtrosApi)
  const series = useSeries(filtrosApi)
  const severidades = useSeveridades()
  const categorias = useCategoriasIncidente()
  const organizaciones = useOpcionesOrganizaciones(esStaff)

  const campos: CampoFiltro<Clave>[] = [
    { nombre: 'desde', etiqueta: 'Desde', tipo: 'fecha' },
    { nombre: 'hasta', etiqueta: 'Hasta', tipo: 'fecha' },
    ...(esStaff
      ? ([{ nombre: 'organizacionId', etiqueta: 'Organización', tipo: 'seleccion', textoTodos: 'Todas', opciones: (organizaciones.data ?? []).map((o) => ({ valor: String(o.id), texto: o.razonSocial })) }] as CampoFiltro<Clave>[])
      : []),
    { nombre: 'severidadId', etiqueta: 'Severidad', tipo: 'seleccion', textoTodos: 'Todas', opciones: (severidades.data ?? []).map((s) => ({ valor: String(s.id), texto: s.nombre })) },
    { nombre: 'categoriaId', etiqueta: 'Categoría', tipo: 'seleccion', textoTodos: 'Todas', opciones: (categorias.data ?? []).map((c) => ({ valor: String(c.id), texto: c.nombre })) },
  ]

  const errorRango = [indicadores.error, series.error].find((e) => isApiError(e) && e.status === 400)
  const s = series.data

  return (
    <>
      <PageHeading>Dashboard</PageHeading>
      <p className="introduccion">
        Indicadores y gráficos del período elegido (por defecto, los últimos 12 meses).
        {usuario?.rol === 'Cliente' && ' Solo incluyen los datos de tu organización.'}
        {usuario?.rol === 'Analista' && ' Solo incluyen los incidentes que tienes asignados.'}
      </p>

      <FiltrosListado
        // key: al cargar los catalogos o cambiar la URL se reinicia el borrador del formulario
        key={`${claveFormulario}-${severidades.data?.length ?? 0}-${categorias.data?.length ?? 0}-${organizaciones.data?.length ?? 0}`}
        etiqueta="Filtrar dashboard"
        valores={valores}
        onAplicar={aplicar}
        campos={campos}
      />

      {errorRango && <p className="alerta alerta--error" role="alert">{mensajeDeError(errorRango)}</p>}

      {indicadores.isPending && <LoadingState mensaje="Calculando indicadores…" />}
      {indicadores.isError && !errorRango && <ErrorState error={indicadores.error} onReintentar={() => indicadores.refetch()} />}

      {indicadores.data && (
        <section aria-labelledby="titulo-indicadores" aria-busy={indicadores.isFetching}>
          <h2 id="titulo-indicadores" className="grafico__titulo">Indicadores</h2>
          <ul className="indicadores">
            {indicadores.data.items.map((i) => (
              <li key={i.clave} className="indicador">
                <p className="indicador__nombre">{i.nombre}</p>
                <p className="indicador__valor">{textoValor(i)}</p>
                <p className="indicador__variacion">{textoVariacion(i)}</p>
                <p className="indicador__formula">Fórmula: {i.formula}</p>
              </li>
            ))}
          </ul>
        </section>
      )}

      {series.isPending && <LoadingState mensaje="Cargando gráficos…" />}
      {series.isError && !errorRango && <ErrorState error={series.error} onReintentar={() => series.refetch()} />}

      {s && (
        <div className="graficos" aria-busy={series.isFetching}>
          <GraficoBarras titulo="Incidentes por mes" descripcion={conclusion(s.incidentesPorMes, 'Mes con más incidentes')} puntos={s.incidentesPorMes} />
          <GraficoLinea titulo="Cumplimiento del SLA por mes" descripcion={conclusion(s.cumplimientoSlaPorMes, 'Mejor mes')} puntos={s.cumplimientoSlaPorMes} unidad=" %" />
          <GraficoBarrasHorizontales titulo="Incidentes por severidad" descripcion={conclusion(s.porSeveridad, 'Severidad más frecuente')} puntos={s.porSeveridad} />
          <GraficoBarrasHorizontales titulo="Incidentes por categoría" descripcion={conclusion(s.porCategoria, 'Categoría más frecuente')} puntos={s.porCategoria} />
          {esStaff && <GraficoBarrasHorizontales titulo="Incidentes por organización" descripcion={conclusion(s.porOrganizacion, 'Organización con más incidentes')} puntos={s.porOrganizacion} />}
          {esStaff && <GraficoBarrasHorizontales titulo="Carga abierta por analista" descripcion={conclusion(s.cargaPorAnalista, 'Mayor carga')} puntos={s.cargaPorAnalista} />}
        </div>
      )}

      {puede(usuario, 'resumirConIa') && indicadores.data && s && <ResumenIa indicadores={indicadores.data} series={s} />}
    </>
  )
}
