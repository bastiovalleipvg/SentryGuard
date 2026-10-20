import { expect, test } from '@playwright/test'

const API = process.env.VITE_API_URL ?? 'https://localhost:7166'
const ENDPOINT_PUBLICO = `${API}/api/v1/catalogos/severidades`

test.describe('CORS aplicado por el navegador', () => {
  test('el origen del frontend puede consumir la API', async ({ page }) => {
    await page.goto('/login')
    const estado = await page.evaluate(async (url) => (await fetch(url)).status, ENDPOINT_PUBLICO)
    expect(estado).toBe(200)
  })

  test('un origen no permitido es bloqueado por el navegador', async ({ page }) => {
    // about:blank tiene un origen opaco ("null"), que no esta en Cors:AllowedOrigins.
    await page.goto('about:blank')
    const resultado = await page.evaluate(async (url) => {
      try {
        await fetch(url)
        return 'permitido'
      } catch (error) {
        return `bloqueado: ${(error as Error).message}`
      }
    }, ENDPOINT_PUBLICO)
    expect(resultado).toMatch(/^bloqueado/)
  })
})
