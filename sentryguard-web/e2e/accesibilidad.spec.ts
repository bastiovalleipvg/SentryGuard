import { AxeBuilder } from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'
import { mkdirSync, writeFileSync } from 'node:fs'
import { iniciarSesion } from './helpers'

const EVIDENCIAS = '../docs/evidencias'
const CAPTURAS = `${EVIDENCIAS}/capturas`
const ETIQUETAS_WCAG = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']

const VIEWPORTS = [
  { nombre: 'movil-320', width: 320, height: 720 },
  { nombre: 'movil-360', width: 360, height: 780 },
  { nombre: 'movil-390', width: 390, height: 844 },
  { nombre: 'tableta-768', width: 768, height: 1024 },
  { nombre: 'escritorio-1280', width: 1280, height: 900 },
  { nombre: 'escritorio-1440', width: 1440, height: 900 },
]

const reporteAxe: Record<string, unknown> = {}

async function auditar(page: Page, vista: string) {
  const resultado = await new AxeBuilder({ page }).withTags(ETIQUETAS_WCAG).analyze()
  reporteAxe[vista] = {
    url: page.url(),
    violaciones: resultado.violations.map((v) => ({ id: v.id, impacto: v.impact, descripcion: v.description, nodos: v.nodes.length })),
    reglasAprobadas: resultado.passes.length,
  }
  expect(resultado.violations, `Violaciones WCAG en ${vista}`).toEqual([])
}

async function capturar(page: Page, vista: string) {
  mkdirSync(CAPTURAS, { recursive: true })
  for (const vp of VIEWPORTS) {
    await page.setViewportSize({ width: vp.width, height: vp.height })
    // Sin scroll horizontal en ningun ancho (WCAG 1.4.10 Reflow)
    const desborde = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(desborde, `Scroll horizontal en ${vista} a ${vp.width}px`).toBeLessThanOrEqual(0)
    await page.screenshot({ path: `${CAPTURAS}/${vista}-${vp.nombre}.png`, fullPage: true })
  }
  await page.setViewportSize({ width: 1280, height: 900 })
}

test.describe('Accesibilidad (axe, WCAG 2.2 AA) y diseño responsivo', () => {
  test.afterAll(() => {
    mkdirSync(EVIDENCIAS, { recursive: true })
    writeFileSync(`${EVIDENCIAS}/axe-reporte.json`, JSON.stringify({ fecha: new Date().toISOString(), etiquetas: ETIQUETAS_WCAG, vistas: reporteAxe }, null, 2))
  })

  // Un solo inicio de sesion por archivo: el login de la API admite 5 intentos por minuto por IP.
  test('vistas principales sin violaciones, sin scroll horizontal y operables con teclado', async ({ page }) => {
    test.setTimeout(240_000)
    await page.goto('/login')
    await auditar(page, 'login')
    await capturar(page, 'login')

    await iniciarSesion(page, 'admin@sentryguard.cl')

    // Teclado: tras una carga completa, el primer Tab llega al enlace para saltar al contenido.
    await page.reload()
    await expect(page.getByRole('heading', { level: 1, name: 'Incidentes' })).toBeVisible()
    await page.keyboard.press('Tab')
    const saltar = page.getByRole('link', { name: 'Saltar al contenido principal' })
    await expect(saltar).toBeFocused()
    await expect(saltar).toBeVisible()
    await page.keyboard.press('Enter')
    await expect(page.locator('#contenido')).toBeFocused()
    await page.keyboard.press('Tab')
    const enfocado = page.locator(':focus')
    await expect(enfocado).toHaveText(/Nuevo incidente/)
    expect(await enfocado.evaluate((el) => getComputedStyle(el).outlineStyle)).not.toBe('none')

    await expect(page.locator('tbody tr, .tarjeta').first()).toBeVisible()
    await auditar(page, 'listado')
    await capturar(page, 'listado')

    await page.locator('tbody tr a, .tarjeta a').first().click()
    await expect(page.getByRole('heading', { level: 2, name: 'Detalles' })).toBeVisible()
    await auditar(page, 'detalle')
    await capturar(page, 'detalle')

    await page.getByRole('button', { name: 'Eliminar' }).click()
    await expect(page.getByRole('dialog')).toBeVisible()
    await auditar(page, 'dialogo-eliminar')
    await page.keyboard.press('Escape')
    await expect(page.getByRole('dialog')).toBeHidden()
    await expect(page.getByRole('button', { name: 'Eliminar' })).toBeFocused()

    await page.goto('/incidentes/nuevo')
    await page.getByRole('button', { name: 'Registrar incidente' }).click()
    await expect(page.getByText('El título debe tener al menos 5 caracteres.')).toBeVisible()
    await auditar(page, 'formulario-con-errores')
    await capturar(page, 'formulario-con-errores')

    // Recursos relacionados: cada vista se audita y las principales se capturan en los 4 anchos.
    const vistas: { ruta: string; vista: string; titulo: RegExp | string; capturar?: boolean }[] = [
      { ruta: '/dashboard', vista: 'dashboard', titulo: 'Dashboard', capturar: true },
      { ruta: '/reportes', vista: 'reportes', titulo: 'Reportes', capturar: true },
      { ruta: '/reportes?tipo=gerencial', vista: 'reportes-gerencial', titulo: 'Reportes' },
      { ruta: '/activos', vista: 'activos', titulo: 'Activos', capturar: true },
      { ruta: '/activos/1', vista: 'activo-detalle', titulo: 'Servidor web de producción' },
      { ruta: '/activos/nuevo', vista: 'activo-formulario', titulo: 'Nuevo activo' },
      { ruta: '/organizaciones', vista: 'organizaciones', titulo: 'Organizaciones' },
      { ruta: '/organizaciones/1', vista: 'organizacion-detalle', titulo: 'Empresa Demo SpA', capturar: true },
      { ruta: '/usuarios', vista: 'usuarios', titulo: 'Usuarios', capturar: true },
      { ruta: '/usuarios/nuevo', vista: 'usuario-formulario', titulo: 'Nuevo usuario' },
      { ruta: '/catalogos', vista: 'catalogos', titulo: 'Catálogos', capturar: true },
      { ruta: '/auditoria', vista: 'auditoria', titulo: 'Auditoría' },
    ]
    for (const v of vistas) {
      await page.goto(v.ruta)
      await expect(page.getByRole('heading', { level: 1, name: v.titulo })).toBeVisible()
      await page.waitForLoadState('networkidle')
      await auditar(page, v.vista)
      if (v.capturar) await capturar(page, v.vista)
    }

    // Menu movil desplegado.
    await page.setViewportSize({ width: 320, height: 720 })
    const menu = page.getByRole('button', { name: /menú/i })
    await menu.click()
    await expect(menu).toHaveAttribute('aria-expanded', 'true')
    await expect(page.getByRole('link', { name: 'Auditoría' })).toBeVisible()
    await auditar(page, 'menu-movil')
    await page.screenshot({ path: `${CAPTURAS}/menu-movil-320.png` })
    await page.setViewportSize({ width: 1280, height: 900 })
  })
})
