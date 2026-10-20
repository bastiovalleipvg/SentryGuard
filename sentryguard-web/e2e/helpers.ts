import { expect, type Page } from '@playwright/test'

export const CLAVE_DEMO = 'Admin123!'

export async function iniciarSesion(page: Page, email: string, password = CLAVE_DEMO): Promise<void> {
  await page.goto('/login')
  await page.getByLabel(/correo electrónico/i).fill(email)
  await page.getByLabel(/contraseña/i).fill(password)
  await page.getByRole('button', { name: 'Ingresar' }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Incidentes' })).toBeVisible()
}
