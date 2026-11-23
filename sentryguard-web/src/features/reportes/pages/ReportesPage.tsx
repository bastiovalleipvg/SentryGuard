import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { TIPOS_REPORTE, type TipoReporte } from '../../../api/contracts'
import { EmptyState } from '../../../components/EmptyState'
import { ErrorState, mensajeDeError } from '../../../components/ErrorState'
import { FiltrosListado, type CampoFiltro } from '../../../components/FiltrosListado'
import { Icono } from '../../../components/Icono'
import { ListadoResponsivo } from '../../../components/ListadoResponsivo'
import { LoadingState } from '../../../components/LoadingState'
import { useNotifications } from '../../../components/Notifications'
import { PageHeading } from '../../../components/PageHeading'
import { Pagination } from '../../../components/Pagination'
import { useParametrosListado } from '../../../hooks/useParametrosListado'
import { formatearFecha } from '../../../utils/formato'
import { useAuth } from '../../auth/useAuth'
import { useEstados, useSeveridades } from '../../catalogos/catalogosService'
import { useOpcionesOrganizaciones } from '../../organizaciones/organizacionesService'
import { useExportarReporte, useReporte, type FormatoExportacion } from '../reportesService'

type Clave = 'desde' | 'hasta' | 'organizacionId' | 'severidadId' | 'estadoId'

const NOMBRES: Record<TipoReporte, { titulo: string; descripcion: string }> = {
  operativo: { titulo: 'Operativo', descripcion: 'Detalle de cada incidente del período.' },
  gerencial: { titulo: 'Gerencial', descripcion: 'Totales por organización, severidad y estado.' },
  sla: { titulo: 'SLA', descripcion: 'Cumplimiento del SLA y tiempo de resolución por mes. Exige un rango de fechas.' },
}

const FORMATOS: { formato: FormatoExportacion; texto: string }[] = [
  { formato: 'xlsx', texto: 'Exportar Excel' },
  { formato: 'pdf', texto: 'Exportar PDF' },
  { formato: 'csv', texto: 'Exportar CSV' },
]

const nf = new Intl.NumberFormat('es-CL', { maximumFractionDigits: 2 })

function celda(columna: string, valor: unknown): string {
  if (valor === null || valor === undefined || valor === '') return '—'
  if (typeof valor === 'number') return nf.format(valor)
  if (columna.startsWith('Fecha') && typeof valor === 'string') return formatearFecha(valor)
  return String(valor)
}

export function ReportesPage() {
  const { usuario } = useAuth()
  const { notificar } = useNotifications()
  const esStaff = usuario?.rol === 'Administrador' || usuario?.rol === 'Supervisor'
  const [params, setParams] = useSearchParams()
  const tipoUrl = params.get('tipo')
  const tipo: TipoReporte = (TIPOS_REPORTE as readonly string[]).includes(tipoUrl ?? '') ? (tipoUrl as TipoReporte) : 'operativo'

  const { filtros, valores, aplicar, cambiarPagina, hayFiltros, claveFormulario } = useParametrosListado<Clave>(
    ['desde', 'hasta', 'organizacionId', 'severidadId', 'estadoId'],
    ['organizacionId', 'severidadId', 'estadoId'],
  )
  const consulta = useReporte(tipo, filtros)
  const exportar = useExportarReporte()
  const severidades = useSeveridades()
  const estados = useEstados()
  const organizaciones = useOpcionesOrganizaciones(esStaff)
  const datos = consulta.data
  // Fecha de emision del reporte impreso: se fija al abrir la vista (no cambia en cada render)
  const [emitidoEn] = useState(() => new Date().toISOString())

  const elegirTipo = (nuevo: TipoReporte) => {
    const siguiente = new URLSearchParams(params)
    siguiente.set('tipo', nuevo)
    siguiente.delete('page')
    setParams(siguiente)
  }

  const campos: CampoFiltro<Clave>[] = [
    { nombre: 'desde', etiqueta: tipo === 'sla' ? 'Desde (obligatorio)' : 'Desde', tipo: 'fecha' },
    { nombre: 'hasta', etiqueta: tipo === 'sla' ? 'Hasta (obligatorio)' : 'Hasta', tipo: 'fecha' },
    ...(esStaff
      ? ([{ nombre: 'organizacionId', etiqueta: 'Organización', tipo: 'seleccion', textoTodos: 'Todas', opciones: (organizaciones.data ?? []).map((o) => ({ valor: String(o.id), texto: o.razonSocial })) }] as CampoFiltro<Clave>[])
      : []),
    { nombre: 'severidadId', etiqueta: 'Severidad', tipo: 'seleccion', textoTodos: 'Todas', opciones: (severidades.data ?? []).map((s) => ({ valor: String(s.id), texto: s.nombre })) },
    { nombre: 'estadoId', etiqueta: 'Estado', tipo: 'seleccion', textoTodos: 'Todos', opciones: (estados.data ?? []).map((e) => ({ valor: String(e.id), texto: e.nombre })) },
  ]

  const descargar = (formato: FormatoExportacion) => {
    const { page: _pagina, ...sinPagina } = filtros
    void _pagina
    exportar.mutate(
      { tipo, formato, filtros: sinPagina },
      {
        onSuccess: (nombre) => notificar(`Archivo generado: ${nombre}`),
        onError: (e) => notificar(mensajeDeError(e), 'error'),
      },
    )
  }

  const filtrosUsados = datos ? Object.entries(datos.filtros).map(([k, v]) => `${k}: ${v}`).join(', ') : ''

  return (
    <>
      <PageHeading>Reportes</PageHeading>
      <p className="introduccion no-imprimir">
        Consulta, exporta e imprime los reportes con los filtros aplicados. Cada exportación queda registrada en la auditoría.
      </p>

      <nav aria-label="Tipo de reporte" className="pestanas no-imprimir">
        <ul className="pestanas__lista">
          {TIPOS_REPORTE.map((t) => (
            <li key={t}>
              <button type="button" className="pestanas__boton" aria-current={t === tipo ? 'page' : undefined} onClick={() => elegirTipo(t)}>
                {NOMBRES[t].titulo}
              </button>
            </li>
          ))}
        </ul>
      </nav>
      <p className="meta no-imprimir">{NOMBRES[tipo].descripcion}</p>

      <div className="no-imprimir">
        <FiltrosListado
          key={`${tipo}-${claveFormulario}-${severidades.data?.length ?? 0}-${estados.data?.length ?? 0}-${organizaciones.data?.length ?? 0}`}
          etiqueta="Filtrar reporte"
          valores={valores}
          onAplicar={aplicar}
          campos={campos}
        />
      </div>

      <div className="acciones acciones--reporte no-imprimir">
        {FORMATOS.map((f) => (
          <button key={f.formato} type="button" className="boton boton--secundario" disabled={exportar.isPending} onClick={() => descargar(f.formato)}>
            <Icono nombre="descargar" />
            {exportar.isPending && exportar.variables?.formato === f.formato ? 'Generando…' : f.texto}
          </button>
        ))}
        <button type="button" className="boton boton--secundario" onClick={() => window.print()}>
          <Icono nombre="imprimir" />
          Imprimir
        </button>
      </div>

      {/* Solo visible al imprimir: titulo, fecha de emision y filtros usados */}
      <div className="solo-impresion">
        <h2>Reporte {NOMBRES[tipo].titulo.toLowerCase()} · SentryGuard</h2>
        <p>Emitido: {formatearFecha(emitidoEn)}</p>
        <p>Filtros: {filtrosUsados || 'ninguno'}</p>
      </div>

      {consulta.isPending && <LoadingState mensaje="Generando reporte…" />}
      {consulta.isError && <ErrorState error={consulta.error} onReintentar={() => consulta.refetch()} />}
      {datos && datos.filas.length === 0 && <EmptyState titulo={hayFiltros ? 'Ningún dato coincide con los filtros' : 'Todavía no hay datos para este reporte'} />}

      {datos && datos.filas.length > 0 && (
        <div aria-busy={consulta.isFetching}>
          <p className="resultado-busqueda" role="status" aria-live="polite">{datos.total} {datos.total === 1 ? 'fila' : 'filas'}.</p>
          <ListadoResponsivo
            items={datos.filas}
            etiqueta={`Reporte ${NOMBRES[tipo].titulo.toLowerCase()}`}
            clave={(f) => `${datos.page}-${datos.columnas.map((c) => String(f[c])).join('|')}`}
            principal={{ encabezado: datos.columnas[0], render: (f) => celda(datos.columnas[0], f[datos.columnas[0]]) }}
            columnas={datos.columnas.slice(1).map((c) => ({ encabezado: c, render: (f: Record<string, unknown>) => celda(c, f[c]) }))}
          />
          <section aria-labelledby="titulo-totales" className="totales-reporte">
            <h2 id="titulo-totales" className="grafico__titulo">Totales</h2>
            <dl className="totales-reporte__lista">
              {Object.entries(datos.totales).map(([k, v]) => (
                <div key={k} className="totales-reporte__item">
                  <dt>{k}</dt>
                  <dd>{nf.format(v)}</dd>
                </div>
              ))}
            </dl>
          </section>
          <div className="no-imprimir">
            <Pagination pagina={datos.page} totalPaginas={datos.totalPages} onCambiar={cambiarPagina} etiqueta="Paginación del reporte" />
          </div>
        </div>
      )}
    </>
  )
}
