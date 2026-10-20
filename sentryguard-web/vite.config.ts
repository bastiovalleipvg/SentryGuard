import react from '@vitejs/plugin-react'
import { existsSync, readFileSync } from 'node:fs'
import { defineConfig, loadEnv, type Plugin } from 'vite'

// Certificado de desarrollo de ASP.NET Core exportado a PEM (npm run certs). Es el mismo
// certificado en el que ya confia el sistema para la API, asi el navegador no muestra avisos.
const CERT = 'certs/localhost.pem'
const KEY = 'certs/localhost.key'
const https = existsSync(CERT) && existsSync(KEY)
  ? { cert: readFileSync(CERT), key: readFileSync(KEY) }
  : undefined

function politicaDeContenido(apiUrl: string): string {
  const apiOrigin = new URL(apiUrl).origin
  return [
    "default-src 'self'",
    "script-src 'self'",
    "style-src 'self'",
    "img-src 'self' data:",
    "font-src 'self'",
    `connect-src 'self' ${apiOrigin}`,
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'none'",
  ].join('; ')
}

// En el build de produccion la CSP va como <meta> dentro del index.html. En desarrollo no se
// aplica porque el servidor de Vite inyecta scripts inline para el recarga en caliente.
function cspEnBuild(csp: string): Plugin {
  // frame-ancestors no es valido en <meta>; se entrega como cabecera en `vite preview`.
  const cspMeta = csp.split('; ').filter((d) => !d.startsWith('frame-ancestors')).join('; ')
  return {
    name: 'sentryguard-csp',
    apply: 'build',
    transformIndexHtml: () => [
      { tag: 'meta', attrs: { 'http-equiv': 'Content-Security-Policy', content: cspMeta }, injectTo: 'head-prepend' },
    ],
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')
  const apiUrl = env.VITE_API_URL || 'https://localhost:7166'
  const csp = politicaDeContenido(apiUrl)

  const cabecerasDeSeguridad = {
    'Content-Security-Policy': csp,
    'X-Content-Type-Options': 'nosniff',
    'X-Frame-Options': 'DENY',
    'Referrer-Policy': 'no-referrer',
    'Permissions-Policy': 'camera=(), microphone=(), geolocation=()',
  }

  return {
    plugins: [react(), cspEnBuild(csp)],
    server: { port: 5173, strictPort: true, https },
    preview: { port: 4173, strictPort: true, https, headers: cabecerasDeSeguridad },
    build: { sourcemap: false },
  }
})
