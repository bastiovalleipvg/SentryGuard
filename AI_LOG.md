# Bitácora de uso de IA generativa

Proyecto: SentryGuard (Trabajo 1: API · Trabajo 2: frontend, consumo de API y seguridad)
Asignatura: Lenguaje de Programación Web II

> **Pendiente de completar por el estudiante** (marcado con ✏️): el plan de cuenta exacto y los
> prompts de las herramientas usadas antes de Claude Code, que no quedaron registrados en el
> repositorio.

## 1. Herramientas, modelos y plan

| Herramienta | Modelo | Plan / tipo de cuenta | Uso |
|---|---|---|---|
| GitHub Copilot (VS Code) | ✏️ completar | ✏️ completar | Primer borrador de la API del Trabajo 1 (endpoints, middleware, DTOs). Se agotó la cuota a mitad del trabajo. |
| Claude (chat en claude.ai) | ✏️ completar | ✏️ completar | Corregir errores de compilación del Trabajo 1 pegando fragmentos de código. |
| Claude Code (aplicación de escritorio, pestaña Code) | Claude Sonnet 5 (`claude-sonnet-5`) | ✏️ Plan de pago de Claude (Claude Code no está disponible en el plan gratuito): indicar Pro o Max | Trabajo 1, pasos 1 a 6 y borrador del informe del Trabajo 1. |
| Claude Code (aplicación de escritorio, pestaña Code) | Claude Opus 5.5 (`claude-opus-5-5`) | ✏️ el mismo plan | Trabajo 2 completo: ajustes de la API, frontend, pruebas, evidencias y documentación. |

Claude Code trabajó directamente sobre el repositorio local (lectura y edición de archivos,
ejecución de `dotnet`, `npm`, Playwright y git) y sobre un navegador integrado para probar la
interfaz. Los commits del Trabajo 1 los hizo el estudiante a mano; en el Trabajo 2 se autorizó a
Claude Code a hacer commits solo en local.

## 2. Reglas de trabajo que el estudiante le impuso a la IA

Estas reglas quedaron en un archivo local de instrucciones (`CLAUDE.md`, fuera de git) y se
fueron agregando a medida que la IA cometía errores:

1. Nunca usar `HasOne<T>()` / `WithMany()` genéricos sin lambda (había creado FKs sombra duplicadas).
2. No confiar en sus propios resúmenes de «esto compila»: correr `dotnet build` y leer el resultado.
3. Todo endpoint de escritura con autorización por rol; todo acceso a Incidente/Activo validado contra la organización del usuario.
4. Mostrar el diff antes de tocar `SentryGuardDbContext.cs` o los modelos.
5. Nunca hacer `git add` de `CLAUDE.md` (agregada después de que la IA lo commiteara).
6. No ejecutar `git add`/`commit`/`push` (agregada después de un commit mezclado).
7. No avanzar al siguiente paso sin confirmar explícitamente «build limpio, listo para commitear» (agregada porque el estudiante commiteó mientras la IA seguía editando y quedó un commit que no compilaba).

## 3. Prompts relevantes por parte del proyecto

Los prompts se transcriben tal como se escribieron. «Qué se hizo con la respuesta» resume lo
aceptado, corregido o descartado.

### 3.1 Arquitectura y datos (Trabajo 1)

**Prompt (Claude Code, Sonnet 5):**
> Contexto: continuando SentryGuard (API CyberOps) donde quedó. [...] Estado real confirmado (no asumas nada más allá de esto) [...] Falta por hacer, en este orden de prioridad: 1. Seeding de catálogos [...] 2. RBAC de los 4 roles [...] 3. Versionamiento [...] 4. Swagger/Scalar con Bearer visible [...] 5. Seguridad rápida (OWASP API4, API8) [...] 6. Tests de integración [...] 7. Informe técnico [...] Cómo quiero que trabajes: Compílalo tú mismo después de cada cambio importante [...] Si algo del código existente (Copilot) no funciona como esperabas, dímelo explícitamente en vez de reescribirlo silenciosamente [...] Empecemos por el punto 1 (seeding). Dime tu plan antes de escribir código.

Qué se hizo:
- **Aceptado:** sembrar catálogos y usuarios de prueba con `HasData()` en una migración, con un hash de contraseña generado una vez con el mismo `PasswordHasher` de la aplicación.
- **Corregido:** la IA propuso horas de SLA de ejemplo; el estudiante las reemplazó por los valores del caso (severidad 24/12/4/1 h, planes 24/12/4 h).
- **Hallazgo de la IA aceptado:** dos controladores registraban endpoints con el mismo nombre y la API se caía en la primera request.

### 3.2 Autorización por roles (Trabajo 1)

**Prompt:**
> Sí, sigamos con el punto 2 (RBAC), incluyendo el fix de ActivoAPI.

Qué se hizo:
- La IA reportó antes de cambiar nada: registro de usuarios abierto a cualquiera (escalada a Administrador), `organizacion_id` y `usuario_id` fijos en 1, listados sin filtro por organización, y una contradicción entre la especificación y `ValidaAccesoOrganizacion`.
- **Decisiones del estudiante:** registro solo para Administrador (aceptó la recomendación); Supervisor y Analista como personal interno sin organización (aceptó); para activos **corrigió** la propuesta de la IA: crear, editar y eliminar solo Administrador y Supervisor (la IA proponía incluir al Cliente).
- **Hallazgo crítico aceptado:** al probar en vivo, la IA detectó que las respuestas filtraban `password_hash` por las navegaciones de EF Core; se corrigió conectando los DTOs que ya existían pero no se usaban.

### 3.3 Manejo de git (correcciones del estudiante)

**Prompts:**
> nunca hagas git add de CLAUDE.md, ni siquiera con -f, está en .gitignore a propósito

> git reset --soft HEAD~2 tire este para los ultimos dos commits, tiralos de nuevo

> Regla importante que se me olvido reforzar: de ahora en adelante, NUNCA me digas que continue con el siguiente punto hasta que tu mismo me confirmes explicitamente 'build limpio, listo para commitear, este es el mensaje sugerido'. [...]

Qué se hizo:
- La IA había commiteado `CLAUDE.md`; el estudiante lo revirtió y fijó la regla.
- Ante `HEAD~2`, la IA advirtió que también se perdería un commit legítimo anterior (DTOs) y el estudiante **corrigió** a `HEAD~1`.
- La IA detectó que un commit del estudiante (`0aea628`) no compilaba porque capturó código a medio terminar; el estudiante pidió corregirlo con `--amend` y verificar que compilara solo.

### 3.4 Versionamiento, documentación y seguridad básica (Trabajo 1)

**Prompts:**
> Sigamos con el punto 3, versionamiento /api/v1/

> Borra WeatherForecastController.cs y el WeatherForecast.cs asociado si existe, ya que confirmaste que es codigo muerto del scaffold.

> Sigamos con el punto 4, Swagger/Scalar con Bearer visible

> Sigamos con el punto 5, CORS y headers de seguridad

Qué se hizo:
- **Aceptado:** prefijo `/api/v1/` en rutas y cabeceras `Location`; esquema Bearer en OpenAPI aplicado solo a endpoints protegidos.
- **Corregido por la IA durante el trabajo:** el código inicial de OpenAPI usaba la API de `Microsoft.OpenApi` v1 y no compilaba con la v2 instalada; se inspeccionó el paquete y se reescribió.
- **Decisión del estudiante:** hacer CORS, cabeceras y rate limiting en el mismo paso, en un solo commit (la IA explicó que `Program.cs` no se podía dividir limpiamente en dos commits).

### 3.5 Pruebas de integración (Trabajo 1)

**Prompt:**
> Commit listo, sigamos con el punto 6

Qué se hizo:
- **Aceptado:** `WebApplicationFactory` contra una BD SQL Server real de prueba en vez de mocks.
- **Hallazgo:** el rate limiting del login hacía fallar las pruebas (todas comparten IP); se desactivó solo en el entorno `Testing`.

### 3.6 Trabajo 2 completo (Claude Code, Opus 5.5)

**Prompt:**
> @"C:\Users\victo\Desktop\Trabajo2-LPW2.docx"
> haz todo. solo en local, si pide commits hazlos y mas tarde arreglamos el repo.

Qué hizo la IA, por área (cada punto quedó en un commit propio):

| Área | Resultado | Correcciones durante el trabajo |
|---|---|---|
| Ajustes a la API | Errores genéricos, login sin enumeración, validación 400 por campo, filtros, paginación acotada, 429 con mensaje, CORS al origen de Vite, CSP en `/api`, versión parchada de una dependencia vulnerable (ver `docs/CAMBIOS_API_TRABAJO1.md`). | `dotnet build` mostró una vulnerabilidad transitiva alta en `System.Security.Cryptography.Pkcs`; se fijó la versión 10.0.13. |
| Arquitectura frontend | Vite + React 19 + TypeScript por features, React Router, TanStack Query, React Hook Form + Zod, cliente HTTP único con validación de respuestas. | Varias librerías (React Router 8, MSW 3, Vitest 5, Zod 4.6) eran más nuevas que el conocimiento del modelo: se revisaron sus tipos en `node_modules` antes de usarlas. |
| Autenticación | JWT en `sessionStorage` con expiración programada, cierre ante 401, `/me` como fuente del rol. | — |
| Accesibilidad | Landmarks, foco en cambios de ruta, `aria-live`, campos con `aria-describedby`, diálogo nativo, contraste ≥ 4.5:1, 0 violaciones axe. | Error encontrado por las pruebas E2E: el foco se movía al `<h1>` también en la carga inicial (StrictMode ejecuta los efectos dos veces), impidiendo llegar al enlace «Saltar al contenido». Se corrigió. |
| Responsivo | Mobile-first, tarjetas bajo 1024 px, tabla en escritorio, objetivos de 44 px. | Las capturas a 768 px mostraron palabras cortadas en la tabla; se cambió el punto de quiebre. |
| Pruebas | 19 de componentes (MSW), 5 E2E con Playwright contra la API real, 8 de integración nuevas en la API. | La opción `onUnhandledRequest` de MSW 2 se ignoraba en silencio en MSW 3 (`onUnhandledFrame`): lo detectó `tsc`. Las E2E chocaban con el rate limiting del login; se redujeron a 2 inicios de sesión por corrida. |
| Datos de demostración | Migración con 3 activos para la organización demo. | Sin ellos un Cliente no podía crear incidentes (no puede crear activos). |
| Evidencias y documentación | Script de verificación de seguridad (19 controles), reportes, capturas, README, informe, presentación, esta bitácora. | La revisión con NVDA no se puede automatizar: quedó como lista para que la complete el estudiante. |

## 4. Qué debe poder explicar el estudiante

Aunque la IA escribió la mayor parte del código, la pauta exige entenderlo y poder modificarlo.
Puntos a repasar antes de la exposición:

- Por qué el token va en `sessionStorage` y qué riesgo se acepta (`src/features/auth/tokenStorage.ts`).
- Cómo `apiRequest` adjunta el JWT, normaliza errores y valida respuestas (`src/api/httpClient.ts`).
- Cómo `ProtectedRoute` y `permisos.ts` deciden qué se ve, y por qué la API vuelve a validar todo.
- Por qué `ValidaAccesoIncidente` y no el id de la URL decide el acceso (OWASP API1).
- Por qué React no ejecuta el `<script>` del título y qué agrega la CSP.
- Cómo se ve el CORS bloqueando un origen no permitido (prueba `e2e/seguridad.spec.ts`).

## 5. Notas del estudiante

✏️ (Agregar aquí qué revisó, entendió o modificó personalmente del código generado.)
