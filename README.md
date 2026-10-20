# SentryGuard — CyberOps Chile SpA

Sistema de gestión de incidentes de ciberseguridad. Monorepo con:

| Carpeta | Contenido | Stack |
|---|---|---|
| `SentryGuard/` | API RESTful (Trabajo 1, con los ajustes del Trabajo 2) | ASP.NET Core .NET 10, EF Core 10, SQL Server, JWT |
| `SentryGuard.Tests/` | Pruebas de integración de la API | xUnit + `WebApplicationFactory` |
| `sentryguard-web/` | Frontend (Trabajo 2) | React 19 + TypeScript + Vite, React Router, TanStack Query, React Hook Form + Zod |
| `scripts/` | Verificación de seguridad contra la API en ejecución | Node.js |
| `docs/` | Informe técnico, cambios a la API, evidencias, presentación | — |
| `AI_LOG.md` | Bitácora de uso de IA generativa | — |

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) y la herramienta de EF Core: `dotnet tool install --global dotnet-ef`
- SQL Server local (Express, Developer o LocalDB)
- Node.js 22.15 o superior y npm (desarrollado con Node 26)
- Git

## Inicio rápido (sin IDE)

Con los requisitos instalados y los User Secrets configurados (sección 1.1), basta con hacer
doble clic en:

| Script | Qué hace |
|---|---|
| `iniciar.bat` | Verifica requisitos, certificado HTTPS y dependencias; compila la API, aplica migraciones, abre la API y el frontend en dos ventanas, espera a que respondan y abre el navegador en `https://localhost:5173` |
| `iniciar.bat preview` | Lo mismo, pero con el build de producción (CSP estricta) en `https://localhost:4173` |
| `detener.bat` | Cierra ambas ventanas y libera los puertos 7166, 5170, 5173 y 4173 |

Si la API o el frontend ya estaban corriendo, `iniciar.bat` los reutiliza.

## 1. API (backend)

### 1.1 Credenciales (no están en el repositorio)

`appsettings.json` no contiene la clave JWT ni la cadena de conexión: se configuran con
[User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), que viven fuera
del repositorio. La plantilla con los nombres de las variables está en `SentryGuard/.env.example`.

```
cd SentryGuard
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=TU_SERVIDOR\TU_INSTANCIA;Database=SentryGuardDB;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_CARACTERES"
```

Clave aleatoria en PowerShell: `[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))`

### 1.2 Certificado HTTPS de desarrollo

La API y el frontend se ejecutan sobre HTTPS con el certificado de desarrollo de ASP.NET Core
(se crea y se marca como confiable una sola vez por equipo):

```
dotnet dev-certs https --trust
```

### 1.3 Base de datos

```
cd SentryGuard
dotnet ef database update
```

Crea `SentryGuardDB` y siembra catálogos, una organización de demostración con 3 activos y
cuatro usuarios de prueba:

| Usuario | Rol | Contraseña |
|---|---|---|
| `admin@sentryguard.cl` | Administrador | `Admin123!` |
| `supervisor@sentryguard.cl` | Supervisor | `Admin123!` |
| `analista@sentryguard.cl` | Analista | `Admin123!` |
| `cliente@sentryguard.cl` | Cliente | `Admin123!` |

### 1.4 Ejecutar

```
cd SentryGuard
dotnet run --launch-profile https
```

- API: `https://localhost:7166` (y `http://localhost:5170`, que redirige a HTTPS)
- Documentación interactiva (solo en Development): `https://localhost:7166/scalar/v1`

## 2. Frontend

```
cd sentryguard-web
npm install
cp .env.example .env        # en PowerShell: Copy-Item .env.example .env
npm run certs               # exporta el certificado de desarrollo a certs/ (una vez)
npm run dev
```

Abre `https://localhost:5173`. La URL de la API se toma de `VITE_API_URL` en `.env`
(por defecto `https://localhost:7166`). Toda variable `VITE_*` es pública: nunca debe contener
llaves ni credenciales.

Build de producción (aplica la Content-Security-Policy estricta):

```
npm run build
npm run preview             # https://localhost:4173
```

## 3. Pruebas

| Suite | Comando | Requiere |
|---|---|---|
| Integración de la API (14) | `dotnet test SentryGuard.slnx` (en la raíz) | SQL Server (usa la BD descartable `SentryGuardDB_Test`) |
| Componentes del frontend (19) | `npm test` (en `sentryguard-web/`) | Nada: la API se simula con MSW |
| End-to-end (5) | `npx playwright install chromium` (una vez) y `npm run test:e2e` | La BD de desarrollo migrada; Playwright levanta la API y el frontend si no están corriendo |
| Verificación de seguridad (19 controles) | `node --use-system-ca scripts/verificar-seguridad.mjs` (en la raíz) | La API corriendo |
| Lint (incluye reglas jsx-a11y) | `npm run lint` | — |
| Auditoría de dependencias | `npm audit` y `dotnet list SentryGuard.slnx package --vulnerable --include-transitive` | — |

El login de la API admite 5 intentos por minuto por IP: el script de seguridad termina
disparando ese límite a propósito, así que espera un minuto antes de correr los E2E o iniciar
sesión otra vez.

Los resultados de la última ejecución están en `docs/evidencias/`.

## 4. Variables de configuración

| Clave | Dónde | Sensible |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | User Secrets / variable de entorno `ConnectionStrings__DefaultConnection` | Sí |
| `Jwt:Key` | User Secrets / variable de entorno `Jwt__Key` | Sí |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpireMinutes` | `appsettings.json` | No |
| `Cors:AllowedOrigins` | `appsettings.json` | No |
| `VITE_API_URL` | `sentryguard-web/.env` | No (es pública por diseño) |

La suite de integración usa su propia clave no sensible en `appsettings.Testing.json`.

## 5. Documentación

- `docs/Informe_Tecnico_Trabajo2.docx` (y su fuente `docs/INFORME_TECNICO.md`): decisiones de diseño y matriz de riesgos.
- `docs/CAMBIOS_API_TRABAJO1.md`: ajustes hechos a la API del Trabajo 1 para consumirla desde el navegador.
- `docs/Presentacion_Trabajo2.pptx`: material de apoyo de la exposición.
- `AI_LOG.md`: bitácora de uso de IA.

## Estructura

```
SentryGuard/
├── SentryGuard/                 API (Endpoints, Services, Repositories, Middleware, Models, Migrations)
├── SentryGuard.Tests/           Integración (IncidenteApiTests, SeguridadApiTests)
├── sentryguard-web/
│   ├── src/
│   │   ├── api/                 Cliente HTTP, errores normalizados, contratos (Zod)
│   │   ├── app/                 Rutas, QueryClient, providers
│   │   ├── components/          Componentes reutilizables (Layout, FormField, ConfirmDialog…)
│   │   ├── features/            auth, incidentes, catalogos, activos, usuarios
│   │   ├── pages/               Acceso denegado, no encontrado, error
│   │   └── test/                MSW (API simulada) y utilidades de prueba
│   └── e2e/                     Playwright (flujo, accesibilidad, CORS)
├── scripts/                     verificar-seguridad.mjs
└── docs/                        Informe, evidencias, presentación
```
