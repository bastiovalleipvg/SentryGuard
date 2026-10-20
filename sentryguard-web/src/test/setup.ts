import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { server } from './server'

// Cualquier peticion sin handler hace fallar el test: ninguna prueba toca una API real.
beforeAll(() => server.listen({ onUnhandledFrame: 'error' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
  sessionStorage.clear()
})
afterAll(() => server.close())

// jsdom no implementa matchMedia: por defecto se simula una pantalla movil (vista de tarjetas).
if (!window.matchMedia) {
  window.matchMedia = (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  })
}

// jsdom no implementa showModal/close de <dialog>.
function showModal(this: HTMLDialogElement) {
  this.setAttribute('open', '')
}
function close(this: HTMLDialogElement) {
  this.removeAttribute('open')
}
if (typeof HTMLDialogElement !== 'undefined' && !HTMLDialogElement.prototype.showModal) {
  HTMLDialogElement.prototype.showModal = showModal
  HTMLDialogElement.prototype.close = close
}
