# sentryguard-web

Frontend de SentryGuard (Trabajo 2). Consume la API del Trabajo 1 (`../SentryGuard`).
Las instrucciones completas del sistema están en el [README de la raíz](../README.md).

## Puesta en marcha

```
npm install
cp .env.example .env        # PowerShell: Copy-Item .env.example .env
npm run certs               # una vez: exporta el certificado HTTPS de desarrollo de .NET
npm run dev                 # https://localhost:5173
```

La API debe estar corriendo en la URL de `VITE_API_URL` (por defecto `https://localhost:7166`).

## Scripts

| Script | Qué hace |
|---|---|
| `npm run dev` | Servidor de desarrollo con HTTPS |
| `npm run build` / `npm run preview` | Build de producción con CSP y vista previa en `https://localhost:4173` |
| `npm run typecheck` | Verificación de tipos |
| `npm run lint` | oxlint, incluido el plugin `jsx-a11y` |
| `npm test` | Pruebas de componentes (Vitest + Testing Library + MSW) |
| `npm run test:e2e` | Pruebas end-to-end con Playwright contra la API real |
| `npm run audit` | `npm audit` con umbral alto |

## Organización

- `src/api/`: `apiRequest` (único cliente HTTP: URL base, Bearer, errores normalizados en `ApiError`, validación de respuestas con Zod) y los contratos de la API.
- `src/features/<recurso>/`: servicio (llamadas HTTP), hooks de TanStack Query, componentes y páginas de cada recurso.
- `src/components/`: componentes reutilizables y accesibles.
- `src/app/routes.tsx`: rutas; `ProtectedRoute` exige sesión y, opcionalmente, roles.
