# SentryGuard (CyberOps Chile SpA)

Sistema de gestión de incidentes de ciberseguridad. Monorepo con:

| Carpeta | Contenido | Stack |
|---|---|---|
| `SentryGuard/` | API RESTful (Trabajo 1, con los ajustes del Trabajo 2) | ASP.NET Core .NET 10, EF Core 10, SQL Server, JWT |
| `SentryGuard.Tests/` | Pruebas de integración de la API | xUnit + `WebApplicationFactory` |
| `sentryguard-web/` | Frontend (Trabajo 2) | React 19 + TypeScript + Vite, React Router, TanStack Query, React Hook Form + Zod |
| `scripts/` | Verificación de seguridad contra la API en ejecución | Node.js |
| `docs/` | Informe técnico, cambios a la API, evidencias, presentación | Markdown, Word y PowerPoint |
| `AI_LOG.md` | Bitácora de uso de IA generativa | Markdown |

> **Evaluación 3 (proyecto integrador):** la documentación del Hito 1 (dominio, requerimientos, arquitectura, modelo de datos, contrato de la API, maquetas y plan de trabajo) está en [docs/evaluacion3/](docs/evaluacion3/README.md).

## Ejecución con Docker (recomendada)

Solo requiere [Docker](https://docs.docker.com/get-docker/) con Compose v2 y unos 2 GB de RAM libres para SQL Server.

```
./scripts/generar-env.ps1        # Windows (en Linux o macOS: ./scripts/generar-env.sh); crea .env con claves aleatorias
docker compose up --build
```

Cuando termina, la aplicación está en **http://localhost:8088** (cambia el puerto con `WEB_PORT` en `.env`). La primera vez tarda
unos minutos: construye las imágenes, la API crea la base con las migraciones y el contenedor `seed` genera 520 incidentes de 12 meses.

| Servicio | Qué es | Puerto |
|---|---|---|
| `web` | nginx sin privilegios con la SPA; reenvía `/api` a la API | 8088 (publicado) |
| `api` | API .NET 10 (imagen *chiseled*, sin shell, usuario sin privilegios) | 8080 (solo red interna) |
| `db` | SQL Server 2022 con volumen `sqldata` | 1433 (solo red interna; con `compose.override.yaml` también en `127.0.0.1:14333`) |
| `seed` | Genera los datos sintéticos y termina | — |

- Usuarios de prueba (datos sintéticos): `admin@sentryguard.cl`, `supervisor@sentryguard.cl`, `analista@sentryguard.cl` y `cliente@sentryguard.cl`, contraseña `Admin123!`. **Solo para la demostración.**
- Documentación interactiva de la API: http://localhost:8088/scalar/v1 (se apaga con `DOCUMENTACION_HABILITADA=false`).
- Estado de la API: http://localhost:8088/health
- `.env` no se versiona. Sin `SA_PASSWORD` y `JWT_KEY` el compose se niega a arrancar. La IA es opcional: sin `IA_API_KEY` se usa el asistente simulado.
- Respaldo y restauración: `SA_PASSWORD=<la de .env> ./scripts/respaldo-restauracion.ps1`.
- Para empezar de cero (borra la base): `docker compose down -v`.

## Uso de IA en el desarrollo

| Herramienta | Propósito |
|---|---|
| Claude Code (modelos Claude Sonnet 5 y Claude Opus 5.5, de Anthropic) | Apoyo en el diseño, el código, las pruebas y la documentación. Todo lo generado fue revisado y probado por el equipo |
| GitHub Copilot y Claude en claude.ai | Apoyo puntual al inicio del Trabajo 1 |

El detalle de los prompts y de lo que se aceptó, corrigió o descartó está en [AI_LOG.md](AI_LOG.md). La IA *dentro del producto* (clasificar incidentes y resumir el dashboard) se describe aparte, en [docs/evaluacion3/](docs/evaluacion3/README.md).

## Funcionalidades

| Módulo | Qué permite | Tablas | Quién |
|---|---|---|---|
| Incidentes (recurso principal) | CRUD con paginación, búsqueda y filtros; cambio de estado y asignación de analista | `tb_Incidente` | Ver: todos (Cliente solo su organización, Analista solo lo asignado). Crear: Admin, Supervisor, Cliente. Editar y cambiar estado: Admin, Supervisor. Eliminar: Admin |
| Bitácora del incidente | Historial con entradas automáticas (creación, edición, cambio de estado, asignación) y notas manuales | `tb_BitacoraIncidente` | Ver: quien ve el incidente. Agregar: Admin, Supervisor, Analista asignado |
| Evidencias | Referencias a archivos (`https://` o ruta relativa) | `tb_EvidenciaIncidente` | Igual que la bitácora |
| Cumplimiento del SLA | Plazo, tiempo transcurrido, fecha límite y estado; usa el contrato SLA vigente o, si no hay, la severidad | `tb_ContratoSLA_Detalle`, `tb_Severidad` | Quien ve el incidente |
| Activos | CRUD con filtros por nombre/IP, categoría y organización; incidentes de cada activo | `tb_Activo` | Ver: Admin y Supervisor todos; Cliente los de su organización; Analista los de sus incidentes asignados. Crear/editar: Admin, Supervisor. Eliminar: Admin (409 si tiene incidentes) |
| Organizaciones | CRUD con RUT validado (dígito verificador); detalle con contratos SLA, activos y usuarios | `tb_Organizacion` | Listar y ver: Admin y Supervisor; el Cliente ve la suya («Mi organización»); el Analista no tiene acceso. Gestionar: Admin |
| Contratos SLA | Vigencia y horas por severidad; finalizar y eliminar; sin períodos superpuestos | `tb_ContratoSLA`, `tb_ContratoSLA_Detalle` | Ver: como la organización. Gestionar: Admin |
| Usuarios | Listado con filtros, alta con política de contraseñas, edición de rol/organización y desactivación | `tb_Usuario`, `tb_Rol` | Admin |
| Catálogos | Severidades, categorías de activo, planes de servicio (crear, editar, eliminar si no están en uso) y estados (solo crear) | `tb_Severidad`, `tb_CategoriaActivo`, `tb_PlanServicio`, `tb_EstadoIncidente` | Admin |
| Auditoría | Registro de cada escritura en la API (usuario, acción, IP, fecha) | `tb_LogAuditoria` | Admin |

La interfaz oculta lo que el rol no puede hacer, pero la API valida el rol y la organización en
cada endpoint: forzar una URL o una petición devuelve 403.

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
| Integración de la API (23) | `dotnet test SentryGuard.slnx` (en la raíz) | SQL Server (usa la BD descartable `SentryGuardDB_Test`) |
| Componentes del frontend (32) | `npm test` (en `sentryguard-web/`) | Nada: la API se simula con MSW |
| End-to-end (5) | `npx playwright install chromium` (una vez) y `npm run test:e2e` | La BD de desarrollo migrada; Playwright levanta la API y el frontend si no están corriendo |
| Verificación de seguridad (29 controles) | `node --use-system-ca scripts/verificar-seguridad.mjs` (en la raíz) | La API corriendo |
| Lint (incluye reglas jsx-a11y) | `npm run lint` | Nada adicional |
| Auditoría de dependencias | `npm audit` y `dotnet list SentryGuard.slnx package --vulnerable --include-transitive` | Nada adicional |

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
├── SentryGuard.Tests/           Integración (IncidenteApiTests, SeguridadApiTests, MantenedoresApiTests)
├── sentryguard-web/
│   ├── src/
│   │   ├── api/                 Cliente HTTP, errores normalizados, contratos (Zod)
│   │   ├── app/                 Rutas, QueryClient, providers
│   │   ├── components/          Componentes reutilizables (Layout, FormField, ConfirmDialog, ListadoResponsivo, FiltrosListado…)
│   │   ├── features/            auth, incidentes, activos, organizaciones, usuarios, catalogos, auditoria
│   │   ├── pages/               Acceso denegado, no encontrado, error
│   │   └── test/                MSW (API simulada) y utilidades de prueba
│   └── e2e/                     Playwright (flujo, accesibilidad, CORS)
├── scripts/                     verificar-seguridad.mjs
└── docs/                        Informe, evidencias, presentación
```
