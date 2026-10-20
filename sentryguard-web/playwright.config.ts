import { defineConfig, devices } from '@playwright/test'

/**
 * Pruebas end-to-end contra el sistema real: la API SentryGuard (HTTPS, base de datos de
 * desarrollo con los usuarios sembrados) y el frontend en modo desarrollo. Si ya estan
 * corriendo, se reutilizan; si no, Playwright los levanta.
 *
 * El login de la API tiene rate limiting (5 intentos por minuto por IP): la suite inicia sesion
 * solo 3 veces y corre en serie para no superarlo.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: 'https://localhost:5173',
    ignoreHTTPSErrors: true,
    locale: 'es-CL',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --launch-profile https --project ../SentryGuard',
      url: 'https://localhost:7166/api/v1/catalogos/roles',
      ignoreHTTPSErrors: true,
      reuseExistingServer: true,
      timeout: 180_000,
    },
    {
      command: 'npm run dev',
      url: 'https://localhost:5173',
      ignoreHTTPSErrors: true,
      reuseExistingServer: true,
      timeout: 60_000,
    },
  ],
})
