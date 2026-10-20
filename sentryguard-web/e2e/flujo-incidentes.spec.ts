import { expect, test } from '@playwright/test'
import { iniciarSesion } from './helpers'

// El login de la API admite 5 intentos por minuto por IP: este archivo inicia sesion una sola vez.
test.describe('Flujo principal contra la API real', () => {
  test('sin sesión, una ruta privada redirige al login', async ({ page }) => {
    await page.goto('/incidentes')
    await expect(page).toHaveURL(/\/login$/)
    await expect(page.getByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeVisible()
  })

  test('Cliente inicia sesión, crea un incidente, lo encuentra en el listado y no puede editarlo', async ({ page }) => {
    const titulo = `E2E acceso sospechoso ${Date.now()}`
    // El titulo incluye un script: debe guardarse y mostrarse como texto, nunca ejecutarse.
    const tituloConScript = `${titulo} <script>window.xssEjecutado=true</script>`

    await iniciarSesion(page, 'cliente@sentryguard.cl')

    // Crear
    await page.getByRole('link', { name: 'Nuevo incidente' }).first().click()
    await expect(page.getByRole('heading', { level: 1, name: 'Nuevo incidente' })).toBeVisible()
    await page.getByLabel(/título/i).fill(tituloConScript)
    await page.getByLabel(/descripción/i).fill('Múltiples inicios de sesión fallidos desde una IP desconocida.')
    await page.getByLabel(/activo afectado/i).selectOption({ label: 'Firewall perimetral (Red)' })
    await page.getByLabel(/severidad/i).selectOption({ label: 'Alta (SLA 4 h)' })
    await page.getByRole('button', { name: 'Registrar incidente' }).click()

    // Detalle del incidente recien creado
    await expect(page).toHaveURL(/\/incidentes\/\d+$/)
    const urlDetalle = page.url()
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(tituloConScript)
    await expect(page.getByText('Incidente registrado correctamente.')).toBeVisible()
    expect(await page.evaluate(() => (window as unknown as { xssEjecutado?: boolean }).xssEjecutado)).toBeUndefined()

    // Listar, filtrando por el titulo
    await page.getByRole('link', { name: 'Volver al listado' }).click()
    await page.getByLabel('Buscar por título').fill(titulo)
    await page.getByRole('button', { name: 'Aplicar filtros' }).click()
    await expect(page.getByText('1 incidente encontrado.')).toBeVisible()
    await expect(page.getByRole('link', { name: tituloConScript })).toBeVisible()
    await expect(page.getByRole('cell', { name: 'Firewall perimetral' })).toBeVisible()

    // Un Cliente no edita incidentes: la ruta lo lleva a "Acceso denegado".
    await page.goto(`${urlDetalle}/editar`)
    await expect(page).toHaveURL(/\/acceso-denegado$/)
    await expect(page.getByRole('heading', { level: 1, name: 'Acceso denegado' })).toBeVisible()
  })
})
