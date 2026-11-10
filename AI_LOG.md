# Bitácora de uso de IA generativa

Proyecto: SentryGuard (Trabajo 1: API · Trabajo 2: frontend, consumo de API y seguridad · Evaluación 3: Plataforma CyberOps)
Asignatura: Lenguaje de Programación Web II
Equipo: Rafael Navarrete · Bastián Ovalle · Adriel Troncoso

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
| Antigravity (Google DeepMind) | Gemini / Advanced Agentic Coding | Entorno de desarrollo asistido | Evaluación 3 (Hito 2): Refactor arquitectura en capas ADR-002, seguridad JWT, ProblemDetails RFC 9457, flujo de estados ADR-008, generador sintético ADR-009 y pruebas unitarias. |

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
| Autenticación | JWT en `sessionStorage` con expiración programada, cierre ante 401, `/me` como fuente del rol. | Sin correcciones. |
| Accesibilidad | Landmarks, foco en cambios de ruta, `aria-live`, campos con `aria-describedby`, diálogo nativo, contraste ≥ 4.5:1, 0 violaciones axe. | Error encontrado por las pruebas E2E: el foco se movía al `<h1>` también en la carga inicial (StrictMode ejecuta los efectos dos veces), impidiendo llegar al enlace «Saltar al contenido». Se corrigió. |
| Responsivo | Mobile-first, tarjetas bajo 1024 px, tabla en escritorio, objetivos de 44 px. | Las capturas a 768 px mostraron palabras cortadas en la tabla; se cambió el punto de quiebre. |
| Pruebas | 19 de componentes (MSW), 5 E2E con Playwright contra la API real, 8 de integración nuevas en la API. | La opción `onUnhandledRequest` de MSW 2 se ignoraba en silencio en MSW 3 (`onUnhandledFrame`): lo detectó `tsc`. Las E2E chocaban con el rate limiting del login; se redujeron a 2 inicios de sesión por corrida. |
| Datos de demostración | Migración con 3 activos para la organización demo. | Sin ellos un Cliente no podía crear incidentes (no puede crear activos). |
| Evidencias y documentación | Script de verificación de seguridad (19 controles), reportes, capturas, README, informe, presentación, esta bitácora. | La revisión con NVDA no se puede automatizar: quedó como lista para que la complete el estudiante. |

### 3.7 Tablas relacionadas y funciones faltantes (Claude Code, Opus 5.5)

**Prompts:**
> te faltan hartas cosas, revisa la pauta, faltan hartas tablas aledañas y funciones requeridas

> haz un plan de lo que llevas y lo que falta, necesito apagar el pc

> Dale con todo

> ignora esto, wena profe, si, hice esto a puro vibecodding, con el proyecto del slep estoy hasta las cachas xD, pongame el 7 porfa xDDDD

El estudiante detectó que el frontend solo operaba incidentes, aunque el modelo tiene 15 tablas.
La IA comparó la pauta y el modelo con la interfaz, dejó el plan en un archivo para retomarlo
y luego completó:

| Área | Resultado | Correcciones durante el trabajo |
|---|---|---|
| API | Validación compartida (`Validacion.cs`), CRUD completo de activos y organizaciones, contratos SLA (antes sin endpoints), SLA calculado con el contrato vigente, bitácora automática, evidencias solo `https://` o ruta relativa, administración de usuarios, edición y borrado de catálogos, consulta de auditoría, 409 al borrar con dependencias (ver `docs/CAMBIOS_API_TRABAJO1.md`, filas 15 a 24). | El RUT de la organización de demostración no tiene un dígito verificador válido: la API solo exige el DV cuando el RUT cambia, para no bloquear su edición. El script de seguridad generaba RUT aleatorios inválidos; se corrigió. |
| Frontend | Pantallas de activos, organizaciones (con contratos SLA, activos y usuarios), «Mi organización», usuarios, catálogos y auditoría; paneles de SLA, bitácora y evidencias en el detalle; menú por rol plegable en móvil; componentes reutilizables (`ListadoResponsivo`, `FiltrosListado`, `ConsultaRecurso`, `ResumenErrores`); carga diferida de las vistas nuevas. | Un `<select>` con valor inicial no mostraba la opción si las opciones llegaban después: se vuelve a montar al cargar. React Hook Form convertía `horas.1` en un arreglo: las claves pasaron a `s1`, `s2`. Un control `disabled` no se envía: el rol de la cuenta propia se muestra como texto. oxlint marcó `watch` (incompatible con React Compiler), un `setState` en un efecto y un hook condicional; se corrigieron. |
| Pruebas | 9 de integración nuevas (23 en total), 13 de componentes nuevas (32), auditoría axe en 15 vistas dentro de la misma E2E, 10 controles nuevos en el script de seguridad (29). | El build superaba 500 kB; con `React.lazy` el bundle inicial quedó en 336 kB. |

### 3.8 Rediseño visual (Claude Code, Opus 5.5)

**Prompt:**
> mejora el front, que se vea profesional, limpio y facil de usar

| Área | Resultado | Correcciones durante el trabajo |
|---|---|---|
| Estructura | Barra lateral oscura con íconos (escritorio) y barra superior con menú desplegable (móvil); login dividido con panel de marca; detalle de incidente en dos columnas; resumen de incidentes con tarjetas que filtran. | El resumen se ubica después del encabezado para no cambiar el orden de foco que verifica la E2E de teclado, y se oculta si la API falla para no duplicar el mensaje de error. |
| Estilo | Nueva paleta con contrastes documentados en `global.css`, botones secundarios sobrios, «Eliminar» en rojo suave (rojo sólido solo en el diálogo que confirma), insignias con barras de severidad o punto de estado además del texto, tablas con encabezados compactos, estados vacíos con ícono. | Las reglas generales de `.boton` pisaban las del botón de menú y de cierre de sesión (especificidad); la barra lateral no cubría todo el alto en páginas largas. Se corrigieron tras revisar las capturas. |
| Verificación | 32/32 pruebas de componentes y 5/5 E2E sin cambios en los selectores; axe sin violaciones en las 15 vistas; capturas regeneradas. | Sin correcciones. |

### 3.9 Evaluación 3, Hito 1: propuesta y diseño (Claude Code, Opus 5.5)

**Prompts:**
> Vamos a preparar la Evaluación 3 de Lenguaje de Programación Web II con este proyecto. El enunciado está en […]\Evaluacion3_LPWII.docx: léelo completo […] Primera tarea (solo análisis, sin cambios de código): compara el proyecto con la rúbrica y dame una tabla de brechas […], un plan por hito con riesgos, y las decisiones que debo tomar con tu recomendación. […] Lo que no puedas verificar márcalo como [NO VERIFICADO]. No hagas nada más hasta que apruebe el plan.

> este es el proyecto de la ev2 de bastian, en cual consideras que es mejor hacer la ev3?

> Vamos entonces con el mío […] Sugiero que hagamos hasta el hito 1 (toda la documentacion, se entrega mañana) y luego incorporamos todo lo que hizo Bastian y lo del hito 2 […] deja un archivo .md aparte […] sobre como integrar todo lo bueno y que nos conviene del proyecto de Bastian.

| Área | Resultado | Correcciones o decisiones del equipo |
|---|---|---|
| Análisis | Tabla de brechas contra la rúbrica, plan por hito y 12 decisiones con recomendación. Se detectó una clave JWT antigua en el historial (rotada: se comprobó comparando hashes sin mostrarla) | El estudiante pidió solo análisis antes de cualquier cambio |
| Elección de la base | Comparación de SentryGuard con SentryGuard-2 (fork de Bastián): se eligió SentryGuard y se documentó qué traer del fork. Al revisarlo aparecieron dos fallas de seguridad en SentryGuard (refresh token en texto plano, usuario desactivado con acceso hasta que vence el JWT) que el fork ya tenía resueltas | La decisión final fue del estudiante |
| Documentación del Hito 1 | `docs/evaluacion3/`: dominio, requerimientos e historias de usuario, arquitectura, ADR, modelo de datos, contrato OpenAPI, maquetas y guía de estilos, plan de trabajo, plan de integración | El diagrama ER y el diccionario se generaron con un script desde el *snapshot* de EF Core, para que coincidan con el esquema real; el primer intento duplicaba columnas y omitía las FK sin navegación, y se corrigió. El contrato nuevo se validó con Redocly CLI. Los contrastes de la guía se recalcularon. Las maquetas nuevas usan el CSS real de la aplicación y se verificaron sin desplazamiento horizontal |

### 3.10 Evaluación 3, Hito 2: Seguridad, ProblemDetails y Refactor a Servicios (Antigravity)

**Prompts:**
> vamos desde el comienzo paso 1 a trabajar y pulir lo que haga falta para alcanzar la mejor nota

| Área | Resultado | Decisiones y correcciones |
|---|---|---|
| Seguridad y JWT (Paso 1) | `AuthService.HashearRefreshToken` (SHA-256), `ValidAlgorithms = HmacSha256` en `Program.cs`, filtro `u.Activo` en `ObtenerUsuarioAutenticadoAsync` para revocación inmediata, clave JWT aleatoria de 64 bytes en `SentryGuardApiFactory`. Pruebas de integración añadidas en `SeguridadApiTests`. | La base de datos almacena exclusivamente el hash SHA-256 del refresh token; al cliente se le entrega el token en claro por única vez. Se eliminó la clave fija de `appsettings.Testing.json`. |
| Estandarización de Errores (Paso 2) | Creación de `SentryGuard/Common/ApiResults.cs` bajo estándar RFC 9457 (ProblemDetails) con extensiones `codigo` y `traceId`. Actualización de `GlobalExceptionHandlingMiddleware` y `Validacion.Conflicto`. Compatibilidad nativa en el frontend (`apiError.ts`). | Se preservó la compatibilidad con clientes que lean `mensaje` o `detail`. Se eliminaron las respuestas `BadRequest(new { error })` dispersas. |
| Capa de Servicios y Desacoplamiento (Paso 3) | Creación de `IIncidenteService` / `IncidenteService`, `IActivoService` / `ActivoService`, `IOrganizacionService` / `OrganizacionService` y `ServiceResult<T>`. Los endpoints ahora actúan puramente como adaptadores HTTP delgados. | Se implementó el patrón Repository e Inyección de Dependencias (ADR-002). Se creó la suite de pruebas unitarias `IncidenteServiceUnitTests` con Moq y EF Core InMemory (5/5 pruebas superadas). |

### 3.11 Evaluación 3, Hito 2: Flujo de Estados (ADR-008), Estandarización Frontend y Generador de Datos Sintéticos (ADR-009) (Antigravity)

**Prompts:**
> la segunda luego la primera y luego la tercera pero eso muy al final cuando terminemos de trabajar por hoy

| Área | Resultado | Decisiones y correcciones |
|---|---|---|
| Mejoras Frontend y Flujo de Estados (Paso 5 / ADR-008) | Estandarización de `IncidenteForm.tsx` con `aplicarErroresApi` y `ResumenErrores` (RFC 9457 ProblemDetails). Creación de `sentryguard-web/src/features/incidentes/transiciones.ts` con matriz de transiciones permitidas (`TRANSICIONES_PERMITIDAS`). Integración en `IncidenteDetailPage.tsx` (`CambiarEstadoForm`) para restringir las opciones del select a transiciones válidas. Validación en backend (`IncidenteService.CambiarEstadoAsync`) rechazando transiciones ilegales con 409 `TRANSICION_INVALIDA`. | La interfaz ahora solo ofrece transiciones coherentes con el ciclo de vida del incidente. Se agregaron pruebas unitarias en `IncidenteServiceUnitTests` para verificar transiciones válidas e inválidas. Vitest completó 33/33 pruebas con éxito y `npm run build` compiló en 582 ms con 0 errores. |
| Generador de Datos Sintéticos en C# (Paso 4 / ADR-009) | Creación de `SentryGuard/Data/GeneradorDatosSinteticos.cs`: generador idempotente con semilla fija (seed: 42) que puebla al menos 520 incidentes a lo largo de 12 meses históricos. Crea organizaciones adicionales ("Banco del Sur", "Clínica RedSalud", "Retail Express") y analistas SOC si no existen. Vincula bitácoras coherentes por estado, evidencias forenses y métricas de cumplimiento SLA realistas (85% dentro de SLA). | Se habilitó la ejecución por argumento CLI (`--seed`) para contenedores Docker (`seed`) y vía endpoint administrativo protegido `POST /api/v1/admin/seed-datos`. Se crearon pruebas unitarias en `GeneradorDatosSinteticosTests.cs` verificando volumen (>= 500 incidentes), rango histórico (> 300 días) e idempotencia estricta. |

## 4. Qué debe poder explicar el estudiante

Aunque la IA escribió la mayor parte del código, la pauta exige entenderlo y poder modificarlo.
Puntos a repasar antes de la exposición:

- Por qué el token va en `sessionStorage` y qué riesgo se acepta (`src/features/auth/tokenStorage.ts`).
- Cómo `apiRequest` adjunta el JWT, normaliza errores y valida respuestas (`src/api/httpClient.ts`).
- Cómo `ProtectedRoute` y `permisos.ts` deciden qué se ve, y por qué la API vuelve a validar todo.
- Por qué `ValidaAccesoIncidente` y no el id de la URL decide el acceso (OWASP API1).
- Por qué React no ejecuta el `<script>` del título y qué agrega la CSP.
- Cómo se ve el CORS bloqueando un origen no permitido (prueba `e2e/seguridad.spec.ts`).
- Por qué un Cliente que manda `?organizacionId=` de otra organización igual recibe solo sus activos (`ActivoAPI.Listar`).
- Por qué borrar un activo con incidentes da 409 y no 500 (FK `Restrict` y el control previo en el endpoint).
- Por qué la ubicación de una evidencia solo se muestra como enlace si es `https://` (`EvidenciasPanel.tsx`) y qué rechaza la API.
- Cómo se elige el plazo del SLA: contrato vigente al crear el incidente o, si no hay, la severidad.

## 5. Notas del estudiante

✏️ (Agregar aquí qué revisó, entendió o modificó personalmente del código generado.)
