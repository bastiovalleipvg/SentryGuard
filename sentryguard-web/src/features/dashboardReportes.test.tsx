import { screen, waitFor, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { API } from '../test/fixtures'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

// Hito 4: dashboard (indicadores, graficos accesibles, resumen con IA) y reportes (filtros, exportacion, permisos).

describe('Dashboard', () => {
  it('muestra los 6 indicadores con su fórmula y la variación en texto', async () => {
    renderApp('/dashboard', { sesion: 'admin' })
    expect(await screen.findByText('Volumen de incidentes')).toBeInTheDocument()
    for (const nombre of ['Cumplimiento del SLA', 'Tiempo medio de resolución', 'Tiempo medio de asignación', 'Backlog activo', 'Proporción de incidentes críticos']) {
      expect(screen.getByText(nombre)).toBeInTheDocument()
    }
    expect(screen.getAllByText(/^Fórmula:/)).toHaveLength(6)
    expect(screen.getByText(/▲ \+12 respecto del período anterior/)).toBeInTheDocument()
    expect(screen.getByText(/▼ -2,5 pp respecto del período anterior/)).toBeInTheDocument()
    expect(screen.getByText('Sin datos')).toBeInTheDocument()
  })

  it('cada gráfico tiene título, conclusión escrita y una tabla con los mismos datos', async () => {
    renderApp('/dashboard', { sesion: 'admin' })
    const grafico = (await screen.findByRole('heading', { name: 'Incidentes por mes' })).closest('figure')!

    expect(within(grafico).getByText(/Mes con más incidentes: 2026-10 \(45\)/)).toBeInTheDocument()
    expect(within(grafico).getByRole('img')).toBeInTheDocument()
    expect(within(grafico).getByText('Ver los datos en tabla')).toBeInTheDocument()
    expect(within(grafico).getByRole('rowheader', { name: '2026-10', hidden: true })).toBeInTheDocument()
    expect(screen.getAllByRole('figure').length).toBeGreaterThanOrEqual(4)
  })

  it('cambiar un filtro actualiza la URL', async () => {
    const { user, router } = renderApp('/dashboard', { sesion: 'admin' })
    // Los catalogos cargan despues y reinician el formulario: se espera a que terminen antes de escribir
    await screen.findByRole('option', { name: 'Crítica' })
    const desde = await screen.findByLabelText('Desde')
    await user.type(desde, '2026-01-01')
    await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }))

    await waitFor(() => expect(router.state.location.search).toContain('desde=2026-01-01'))
  })

  it('un rango invertido muestra el error de la API sin romper la pantalla', async () => {
    renderApp('/dashboard?desde=2026-05-01&hasta=2026-01-01', { sesion: 'admin' })
    expect(await screen.findByText(/Rango de fechas inválido/)).toBeInTheDocument()
  })

  it('el resumen con IA se anuncia y lleva la etiqueta "Generado con IA"', async () => {
    const { user } = renderApp('/dashboard', { sesion: 'supervisor' })
    await user.click(await screen.findByRole('button', { name: 'Resumir con IA' }))

    expect(await screen.findByText('Generado con IA')).toBeInTheDocument()
    expect(screen.getByText(/El volumen de incidentes subió/)).toBeInTheDocument()
  })

  it('si la IA falla (502) muestra el mensaje y el resto del dashboard sigue visible', async () => {
    server.use(
      http.post(`${API}/api/v1/ia/resumir`, () =>
        HttpResponse.json({ status: 502, title: 'Servicio de IA no disponible', detail: 'El proveedor de IA devolvió un error.', codigo: 'IA_ERROR_PROVEEDOR' }, { status: 502 }),
      ),
    )
    const { user } = renderApp('/dashboard', { sesion: 'admin' })
    await user.click(await screen.findByRole('button', { name: 'Resumir con IA' }))

    expect(await screen.findByText(/proveedor de IA/i)).toBeInTheDocument()
    expect(screen.getByText('Volumen de incidentes')).toBeInTheDocument()
  })

  it('el Cliente ve el dashboard sin el resumen con IA ni los gráficos de staff', async () => {
    renderApp('/dashboard', { sesion: 'cliente' })
    await screen.findByText('Volumen de incidentes')

    expect(screen.queryByRole('button', { name: 'Resumir con IA' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Incidentes por organización' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Organización')).not.toBeInTheDocument()
  })
})

describe('Reportes', () => {
  it('el Administrador ve la tabla, los totales y puede cambiar de reporte', async () => {
    const { user } = renderApp('/reportes', { sesion: 'admin' })
    expect(await screen.findByText('Ransomware en servidor de archivos')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Totales' })).toBeInTheDocument()

    const nav = screen.getByRole('navigation', { name: 'Tipo de reporte' })
    expect(within(nav).getByRole('button', { name: 'Operativo' })).toHaveAttribute('aria-current', 'page')
    await user.click(within(nav).getByRole('button', { name: 'Gerencial' }))
    await waitFor(() => expect(within(nav).getByRole('button', { name: 'Gerencial' })).toHaveAttribute('aria-current', 'page'))
  })

  it('el reporte de SLA sin rango de fechas muestra el error de la API', async () => {
    renderApp('/reportes?tipo=sla', { sesion: 'admin' })
    expect(await screen.findByText(/filtros del reporte no son válidos/i)).toBeInTheDocument()
  })

  it('exportar pide la ruta del reporte con el formato elegido y los filtros aplicados', async () => {
    let pedida: URL | null = null
    server.use(
      http.get(`${API}/api/v1/reportes/:tipo/exportar`, ({ request }) => {
        pedida = new URL(request.url)
        return new HttpResponse('Id;Titulo', { headers: { 'Content-Type': 'text/csv', 'Content-Disposition': 'attachment; filename="reporte-operativo-2026-11-23.csv"' } })
      }),
    )
    const crear = vi.fn<() => string>(() => 'blob:prueba')
    Object.assign(URL, { createObjectURL: crear, revokeObjectURL: vi.fn<() => void>() })
    const { user } = renderApp('/reportes?severidadId=4', { sesion: 'admin' })

    await screen.findByText('Ransomware en servidor de archivos')
    await user.click(screen.getByRole('button', { name: 'Exportar CSV' }))

    await waitFor(() => expect(pedida).not.toBeNull())
    expect(pedida!.pathname).toBe('/api/v1/reportes/operativo/exportar')
    expect(pedida!.searchParams.get('formato')).toBe('csv')
    expect(pedida!.searchParams.get('severidadId')).toBe('4')
    await waitFor(() => expect(crear).toHaveBeenCalled())
    expect(await screen.findByText(/Archivo generado: reporte-operativo-2026-11-23.csv/)).toBeInTheDocument()
  })

  it('un 429 al exportar se muestra como mensaje de error', async () => {
    server.use(
      http.get(`${API}/api/v1/reportes/:tipo/exportar`, () =>
        HttpResponse.json({ codigo: 'DEMASIADAS_SOLICITUDES', mensaje: 'Demasiadas exportaciones. Espera un minuto.' }, { status: 429 }),
      ),
    )
    const { user } = renderApp('/reportes', { sesion: 'admin' })
    await screen.findByText('Ransomware en servidor de archivos')
    await user.click(screen.getByRole('button', { name: 'Exportar PDF' }))

    expect(await screen.findByText(/Demasiadas exportaciones/)).toBeInTheDocument()
  })

  it('el Analista no ve el enlace y la ruta le muestra acceso denegado', async () => {
    renderApp('/reportes', { sesion: 'analista' })
    expect(await screen.findByRole('heading', { name: /acceso denegado/i })).toBeInTheDocument()
    const menu = screen.getByRole('navigation', { name: 'Principal' })
    expect(within(menu).queryByRole('link', { name: 'Reportes', hidden: true })).not.toBeInTheDocument()
    expect(within(menu).getByRole('link', { name: 'Dashboard', hidden: true })).toBeInTheDocument()
  })
})
