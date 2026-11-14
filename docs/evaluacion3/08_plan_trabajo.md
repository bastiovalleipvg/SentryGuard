# 8. Plan de trabajo

## 8.1 Equipo y responsabilidades

Cada integrante es **responsable** de un área: la lidera y hace sus commits. Todos deben poder explicar cualquier parte en la defensa.

| Integrante | Área principal | Área de apoyo |
|---|---|---|
| **Adriel Troncoso** | Frontend (pantallas nuevas, orden y validación inmediata, dashboard, reportes en pantalla e impresión), accesibilidad y documentación | Integración de IA en la interfaz |
| **Bastián Ovalle** | Backend y datos: integrar SentryGuard-2, capa de servicios, migraciones (CHECK, vistas, auditoría), datos sintéticos, endpoints de dashboard y reportes | Respaldo y restauración |
| **Rafael Navarrete** | Contenerización y CI/CD (Dockerfiles, compose, GitHub Actions, escaneo Trivy), lógica de negocio, pruebas unitarias y endpoint de IA | Manual de usuario |

> Si el equipo queda en 2 integrantes, el área del integrante 3 se reparte: Bastián toma la contenerización y el CI, y Adriel la IA y el plan de despliegue. La pauta exige entre 3 y 4 integrantes, así que hay que confirmarlo con el docente.

## 8.2 Hitos y entregables

Las fechas son estimadas: la pauta indica semanas, no fechas, y el Hito 1 se entrega el 5 de noviembre de 2026. [NO VERIFICADO: confirmar el calendario con el docente.]

| Hito | Semana (aprox.) | Entregables | Responsable principal |
|---|---|---|---|
| **1. Propuesta y diseño** | Sem. 11 (5 nov.) | Todo `docs/evaluacion3/`: dominio, requerimientos e historias de usuario, arquitectura, ADR, modelo ER y diccionario, contrato OpenAPI, maquetas y guía de estilos, este plan | Adriel |
| **2. Datos y backend** | Sem. 12 (12 nov.) | Integración de SentryGuard-2; formato de error único; capa de servicios con pruebas unitarias; migraciones (categoría de incidente, transiciones, auditoría por entidad, CHECK y vistas); orden en las APIs; datos sintéticos (500 incidentes o más en 12 meses); respaldo y restauración probados | Bastián |
| **3. Front, contenedores e IA** | Sem. 13 (19 nov.) | Orden y validación inmediata en la interfaz; transiciones válidas en la interfaz; Dockerfiles, `compose.yaml` y `.env.example`; GitHub Actions; endpoints y pantallas de IA con pruebas simuladas; `/health` | Integrante 3 y Adriel |
| **4. Análisis, reportes y despliegue** | Sem. 14 (26 nov.) | Endpoints y pantalla de dashboard; 3 reportes con exportación a Excel, PDF y CSV e impresión; 6 indicadores o más interpretados; plan de despliegue con los 10 apartados y una reversión probada; NVDA; capturas de 360 a 1440 px; ensayo cronometrado | Adriel y Bastián |
| **5. Entrega y defensa** | Sem. 15 (3 dic.) | Informe técnico en PDF (11 secciones), informe de integración, manual de usuario por rol, presentación, archivos de ejemplo exportados | Todos |

## 8.3 Herramientas

| Propósito | Herramienta |
|---|---|
| Código y revisión | Git y GitHub (repositorio `bastiovalleipvg/SentryGuard`), *pull requests* |
| Tareas | GitHub Issues y Projects: una *issue* por historia de usuario o tarea, con su hito |
| Backend | .NET 10, ASP.NET Core, EF Core 10, SQL Server 2022, xUnit |
| Frontend | Node 26, Vite, React 19, TypeScript, Vitest y MSW, Playwright y axe |
| Contenedores y CI | Docker y Docker Compose, GitHub Actions, Dependabot |
| Diagramas | Mermaid, en el mismo repositorio (`docs/evaluacion3/diagramas/`), exportado a PNG y SVG |
| IA de apoyo al desarrollo | Claude Code (declarado en el README y en `AI_LOG.md`) |

## 8.4 Organización del repositorio

```
SentryGuard/
├── SentryGuard/            API: Endpoints, Services, Repositories, Data, Migrations, Middleware
├── SentryGuard.Tests/      Integración (y desde el Hito 2, unitarias de los servicios)
├── sentryguard-web/        Frontend por features, pruebas de componentes y E2E
├── docs/evaluacion3/       Documentación de la Evaluación 3 (este hito)
├── docs/                   Documentación y evidencias de los Trabajos 1 y 2
├── scripts/                Verificación de seguridad y, más adelante, respaldo y datos sintéticos
└── (Hito 3) compose.yaml, .env.example, Dockerfiles, .github/workflows/
```

**Ramas y flujo de trabajo:**
- `main` siempre compila y pasa las pruebas.
- Cada tarea va en una rama `tipo/descripcion` (por ejemplo, `feat/dashboard-indicadores`) y entra por *pull request*, revisado por otro integrante, con el CI en verde desde el Hito 3.
- Los commits son en español, con prefijo: `feat:`, `fix:`, `docs:`, `test:`, `chore:`.

## 8.5 Definición de terminado

Una tarea está terminada cuando:

1. Compila sin errores ni advertencias nuevas (`dotnet build`, `npm run build`, `npm run lint`).
2. Tiene pruebas: de integración para los endpoints, unitarias para los servicios y de componentes para las pantallas.
3. La pantalla nueva pasa axe sin violaciones y se puede usar a 360 px y solo con el teclado.
4. La documentación de `docs/evaluacion3/` se actualizó si cambió el contrato, el modelo o la arquitectura.
5. El uso de IA de apoyo quedó registrado en `AI_LOG.md`.

## 8.6 Entorno de contenedores

Implementado en `compose.yaml` (y `compose.override.yaml`, solo para desarrollo). Se levanta con `docker compose up --build` después de crear `.env` (`scripts/generar-env.ps1` o `.sh`).

| Servicio | Imagen base (versión fija) | Puerto | Red | Volumen | Health check |
|---|---|---|---|---|---|
| `web` | `nginxinc/nginx-unprivileged:1.28.0-alpine` con el *build* de la SPA (etapa previa `node:22.14.0-alpine3.21`) | 8088 publicado (`WEB_PORT`) | `app` | — | `wget /nginx-health` |
| `api` | `mcr.microsoft.com/dotnet/aspnet:10.0.15-noble-chiseled` (sin shell, usuario sin privilegios); compilada con `dotnet/sdk:10.0.400` en una etapa previa | 8080, solo red interna | `app` y `datos` | — | `dotnet SentryGuard.dll --healthcheck` (consulta `/health`) |
| `db` | `mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04` | 1433, solo red interna (con el *override* de desarrollo, también `127.0.0.1:14333`) | `datos` | `sqldata` (cubre `/var/opt/mssql`, incluida `backups/`) | `sqlcmd SELECT 1` |
| `seed` | La misma imagen de la API, con el argumento `--seed` | — | `datos` | — | Se ejecuta una vez y termina |

Endurecimiento: `read_only`, `tmpfs` en `/tmp`, `cap_drop: ALL` y `no-new-privileges` en `web`, `api` y `seed`; política `unless-stopped`; la red `datos` aísla la base del proxy. La API migra la base al iniciar (`Database__MigrarAlIniciar`), confía en `X-Forwarded-For` del proxy (`Proxy__ConfiarEnEncabezados`) para que el límite de intentos de login cuente por cliente y no por proxy, y no redirige a HTTPS porque el TLS termina fuera de ella (`Seguridad__RedirigirAHttps=false`). La SPA se construye con `VITE_API_URL=same-origin`: llama a la API con rutas relativas y no incrusta URLs ni secretos.

Las variables van en `.env` (excluido del repositorio); `SA_PASSWORD` y `JWT_KEY` no tienen valor por defecto. Se entrega `.env.example` sin valores.

## 8.7 Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Menos de 3 integrantes | Media | Alto | Confirmarlo con el docente en el Hito 1; plan B de reparto (8.1) |
| Aporte desigual en el historial de commits | Media | Alto | Áreas con dueño; cada uno commitea lo suyo; revisión cruzada en los *pull requests* |
| La clave antigua en el historial de git | Resuelto | Alto | Historial reescrito el 4 de noviembre de 2026 (ADR-011) |
| El refactor a servicios rompe funciones existentes | Media | Medio | Refactor por módulo, protegido por las 24 pruebas de integración y las 33 de componentes |
| SQL Server en contenedor (RAM, ARM) | Media | Medio | Documentar el requisito de unos 2 GB de RAM; ensayar la demostración en el equipo que se usará |
| Costo o caída del servicio de IA | Baja | Medio | Adaptador con *timeout* y servicio simulado; la aplicación funciona sin la IA; límite de tasa por usuario |
| Exponer la clave de IA | Baja | Alto | Solo en `.env` y en los secretos de GitHub; el CI usa el servicio simulado; *push protection* de GitHub activado |
| Alcance demasiado amplio para 5 semanas | Alta | Alto | El orden de 2.5 prioriza lo obligatorio (M); lo «S» y lo «C» se recorta primero |
| La documentación no coincide con el código | Media | Alto | Diagramas como código en el repositorio; el diccionario se genera desde el esquema; revisión de `docs/` en cada *pull request* |
