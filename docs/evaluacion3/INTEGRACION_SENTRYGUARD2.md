# Integración de SentryGuard-2 (trabajo de Bastián) en SentryGuard

Después del Trabajo 1, Bastián hizo un fork (`bastiovalleipvg/SentryGuard-2`) y lo usó en el Trabajo 2, mientras que Adriel continuó en el repositorio original. La base de la Evaluación 3 es `SentryGuard` (ver [ADR-001](04_decisiones_adr.md#adr-001--base-del-proyecto-el-repositorio-sentryguard-original)). Este documento dice **qué traer del fork, en qué orden y cómo**, para no perder lo que Bastián resolvió mejor.

**Cuándo:** al comenzar el Hito 2, antes del refactor a servicios. **Quién:** Bastián, para que el aporte quede en sus commits.

## Por qué no se fusionan las ramas

Los dos repositorios divergieron hace semanas. En el frontend tienen arquitecturas y versiones de librerías distintas:

| | SentryGuard | SentryGuard-2 |
|---|---|---|
| Organización | Por *features* | Por páginas |
| Cliente HTTP | `fetch` | Axios |
| Router | React Router 8 | React Router 7 |
| Zod | 4 | 3 |
| Carpeta | `sentryguard-web/` | `frontend/` |

Un `git merge` produciría conflictos en casi todos los archivos. En lugar de eso, **cada pieza se porta a mano**, en un commit o *pull request* propio que diga de dónde viene, por ejemplo: `feat(api): formato de error ProblemDetails (portado de SentryGuard-2)`.

## Qué se trae, en orden de prioridad

### 1. Correcciones de seguridad (obligatorias)

| # | Pieza en SentryGuard-2 | Problema que resuelve en SentryGuard | Cómo portarla |
|---|---|---|---|
| 1.1 | `AuthService.HashearRefreshToken` (SHA-256) y su uso en `UsuarioAPI` (login y refresh) | **Hoy el refresh token se guarda en texto plano** en `tb_RefreshToken.token_hash` (`UsuarioAPI.cs`, `TokenHash = refreshTokenHash`). Quien lea la base puede usarlo | Al emitirlo, guardar solo `SHA256(token)` y devolver el token plano una sola vez. Al refrescar, comparar los hashes. Los tokens que ya existen se revocan con una migración o un script |
| 1.2 | `ObtenerUsuarioAutenticadoAsync` filtra `u.Activo` | **Un usuario desactivado conserva el acceso hasta que vence su JWT** (hasta 60 min) | Agregar `&& u.Activo` a la consulta, y portar la prueba `UsuarioDesactivado_PierdeElAccesoDeInmediato_AunConTokenVigente` |
| 1.3 | `ValidAlgorithms = { HmacSha256 }` en la validación del JWT (`Program.cs`) | Evita que se acepten tokens firmados con otro algoritmo | Una línea en `AddJwtBearer` |
| 1.4 | La clave JWT aleatoria en las pruebas (`SentryGuardApiFactory`: `RandomNumberGenerator.GetBytes(64)`) | Hoy las pruebas usan una clave fija de `appsettings.Testing.json` | Generarla en la *factory* y eliminarla del archivo |

### 2. Formato de error único (Hito 2, RF de errores)

| Pieza | Qué hacer |
|---|---|
| `Common/ApiResults.cs` (ProblemDetails) | Copiarlo a `SentryGuard/Common/` y ampliarlo: agregar la extensión `codigo` y el `traceId`, y los métodos `NoEncontrado`, `Conflicto`, `SinPermiso`, `ErrorServicioExterno` y `TiempoAgotado`, según el modelo `Error` de [contrato-api/openapi-evaluacion3.yaml](contrato-api/openapi-evaluacion3.yaml) |
| — | Reemplazar en todos los endpoints `BadRequest(new { error })`, `Validacion.Conflicto` y el cuerpo de `GlobalExceptionHandlingMiddleware` por `ApiResults` |
| — | Ajustar `ApiError.fromResponse` del frontend para leer `detail` y `codigo` (conserva la compatibilidad con `errors` por campo) |
| `Paginacion` (de `ApiResults.cs`) | Ya existe en `Endpoints/Paginacion.cs`: dejar una sola versión, en `Common/` |

### 3. Dashboard (base para el Hito 4)

| Pieza | Qué hacer |
|---|---|
| `Endpoints/DashboardAPI.cs` (`GET /api/v1/dashboard/resumen`) | Tomar su lógica de visibilidad por rol (Cliente, su organización; Analista, sus asignados) y sus métricas como punto de partida de `IDashboardService`. Cambios: (a) agregar los filtros `desde`, `hasta`, `organizacionId`, `severidadId` y `categoriaId`; (b) calcular en SQL (vistas) en lugar de en memoria, porque hoy carga todas las filas; (c) separar en `/indicadores` y `/series`, según el contrato |
| `pages/PanelPage.tsx` y `panel.css` (gráfico de barras SVG con tabla `sr-only`, medidor de SLA, indicadores, «En vivo») | Portar los componentes de gráfico a `sentryguard-web/src/features/dashboard/components/` (`GraficoBarras`, `Medidor`) con las clases del sistema de diseño (`global.css`); reemplazar la tabla `sr-only` por un `<details>` visible, como en la maqueta |
| `PanelApiTests.cs` (`Dashboard_SinToken_Devuelve401`, `Dashboard_Administrador_DevuelveMetricasCompletas`, `Dashboard_Cliente_SoloVeSuAlcance`) y `test/panel.test.tsx` | Portarlas y adaptarlas a las rutas nuevas |

### 4. Pruebas que no tenemos

| Prueba en SentryGuard-2 | Qué cubre |
|---|---|
| `Login_Exitoso_NoExponePasswordHashNiRefreshTokenEnClaro` | Acompaña a 1.1 |
| `Cors_OrigenDelFrontend_EsPermitidoYNoSeUsaComodin` | Que CORS no use `*` |
| `Catalogos_SinToken_Devuelven401` (Theory) | Revisar primero: en SentryGuard los `GET` de catálogos son públicos a propósito; decidir y documentar |
| `Cliente_NoPuedeLeerOtroUsuario_Devuelve403` | OWASP API1 sobre `/usuarios/{id}` |
| `test/respuestas.test.tsx`, `test/xss.test.tsx` | Comparar con las nuestras y portar solo los casos que falten |

### 5. Calidad y experiencia (opcional, si hay tiempo)

| Pieza | Valor | Cuidado |
|---|---|---|
| `.github/dependabot.yml` | Actualiza las dependencias de npm y NuGet cada semana (OWASP: componentes vulnerables) | Cambiar `directory: /frontend` por `/sentryguard-web`; agregar `github-actions` y `docker` cuando existan |
| `components/ThemeToggle.tsx` y el tema oscuro | Respeta `prefers-color-scheme` | Hay que definir los tokens oscuros en `global.css` y volver a verificar **todos** los contrastes y axe en las 15 vistas |
| `components/Buscador.tsx` (Ctrl/Cmd + K) | Navegación rápida por teclado | Revisar el foco y Esc en el `<dialog>`, y que no choque con los atajos de los lectores de pantalla |
| `playwright.evidence.config.ts` y la suite de evidencia | Regenera las capturas y los reportes con un comando | Unificarla con `e2e/accesibilidad.spec.ts`, que ya hace algo parecido |
| `docs/CHECKLIST_ENTREGA.md` | Lista de verificación de la entrega | Adaptarla a la pauta de la Evaluación 3 |

### Lo que no se trae

- **El frontend completo de SentryGuard-2** (páginas, Axios, React Router 7, Zod 3): duplicaría lo que ya existe en `sentryguard-web/`.
- **Su `AuditoriaAPI` y su gestión de usuarios:** SentryGuard ya los tiene. Antes de descartarlos, comparar los casos que prueba `Auditoria_Administrador_RegistraSusAcciones`.
- **Sus migraciones:** son las mismas tres del Trabajo 1; SentryGuard tiene además `SeedActivosDemo`.

## Procedimiento sugerido

1. ~~Limpiar la clave antigua del historial de SentryGuard~~: hecho el 4 de noviembre de 2026 (ADR-011). Antes de portar nada, cada integrante vuelve a clonar el repositorio.
2. Clonar los dos repositorios uno al lado del otro y crear una rama por bloque: `feat/seguridad-desde-sg2`, `feat/problem-details`, `feat/dashboard-base` y así.
3. Por cada pieza, copiarla, adaptar los nombres y las rutas, y ejecutar **todas** las pruebas: `dotnet test SentryGuard.slnx`, `npm test` y `npm run test:e2e`.
4. Abrir un *pull request* por bloque, que revisa Adriel, y fusionarlo con el CI en verde, cuando exista.
5. En el informe final (sección «Articulación del módulo y cierre» y «Aporte de cada integrante»), enlazar SentryGuard-2 como el trabajo de Bastián en el Trabajo 2 y nombrar las piezas portadas.

## Lista de verificación

- [ ] 1.1 Refresh token con hash, más su prueba
- [ ] 1.2 Usuario desactivado pierde el acceso al instante, más su prueba
- [ ] 1.3 `ValidAlgorithms` en el JWT
- [ ] 1.4 Clave JWT aleatoria en las pruebas
- [ ] 2. `ApiResults` (ProblemDetails) en toda la API y en el frontend
- [ ] 3. Base del dashboard (servicio, componentes de gráfico y pruebas)
- [ ] 4. Pruebas faltantes
- [ ] 5. Dependabot (y, opcionalmente, el tema oscuro y el buscador)
