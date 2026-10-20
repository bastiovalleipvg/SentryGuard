# Informe técnico — Trabajo 2: Frontend, consumo de API y seguridad

**Proyecto:** SentryGuard — gestión de incidentes de ciberseguridad de CyberOps Chile SpA

**Asignatura:** Lenguaje de Programación Web II · **Docente:** Enso Guidotti · **Unidad:** UT2

**Estudiante:** ✏️ (completar) · **Fecha:** 20-10-2026

## 1. Resumen

Se construyó una aplicación web en React que consume la API RESTful del Trabajo 1 sobre el mismo caso de negocio. Un usuario autenticado opera el recurso principal, los incidentes, con listado paginado y filtrable, detalle, creación, edición y eliminación con confirmación, y usa recursos relacionados (catálogos de severidad y estado, activos de su organización y analistas) en formularios y filtros. Lo que cada usuario ve y puede hacer depende de su rol, y la API sigue validando cada acceso. Para el consumo desde el navegador se ajustó la API (detalle en `docs/CAMBIOS_API_TRABAJO1.md`). El sistema funciona sobre HTTPS, la interfaz no presenta violaciones WCAG 2.2 AA detectables con axe y se adapta desde 320 px. Hay 19 pruebas de componentes, 5 end-to-end contra la API real y 14 de integración en la API, todas exitosas.

## 2. Arquitectura del frontend

**Framework y herramientas.** React 19 con TypeScript y Vite 8. TypeScript permite expresar el contrato de la API como tipos y detectar en compilación usos incorrectos; Vite da un servidor de desarrollo con HTTPS y un build optimizado.

**Estructura por features.** Cada recurso agrupa su servicio HTTP, sus hooks de datos, sus componentes y sus páginas:

| Carpeta | Responsabilidad |
|---|---|
| `src/api/` | Cliente HTTP único (`apiRequest`), error normalizado (`ApiError`) y contratos validados con Zod |
| `src/features/auth/` | Sesión (contexto), almacenamiento del token, login, guard de rutas, matriz de permisos |
| `src/features/incidentes/` | Servicio, hooks de TanStack Query, esquema del formulario, componentes y páginas del CRUD |
| `src/features/catalogos`, `activos`, `usuarios` | Recursos relacionados usados en formularios y filtros |
| `src/components/` | Componentes reutilizables: `Layout`, `FormField`, `ConfirmDialog`, `Pagination`, estados de carga, error y vacío, notificaciones |
| `src/app/` | Definición de rutas, `QueryClient` y providers |

**Enrutamiento.** React Router con rutas anidadas: `/login` es pública; todo lo demás cuelga de una ruta protegida que exige sesión y comparte el `Layout`. Las rutas de creación y edición exigen además roles concretos.

**Configuración.** La URL de la API sale de `VITE_API_URL` (`.env`, con plantilla `.env.example`), se valida al arrancar y debe ser HTTPS. Ninguna variable `VITE_*` contiene secretos, porque terminan dentro del bundle.

## 3. Capa de consumo de la API

Toda llamada pasa por `apiRequest` (`src/api/httpClient.ts`), que:

- construye la URL desde `VITE_API_URL`, sin URLs fijas en los componentes;
- adjunta el JWT como `Authorization: Bearer` y envía `credentials: 'omit'` (nunca cookies);
- convierte cualquier respuesta no exitosa en un `ApiError` con estado, código, mensaje para el usuario, errores por campo (del `ValidationProblem` de ASP.NET) y segundos de espera (`Retry-After`);
- ante un 401 de una ruta autenticada, avisa al `AuthProvider` para cerrar la sesión;
- valida el cuerpo de cada respuesta con un esquema Zod y rechaza las que no cumplen el contrato.

Los componentes nunca llaman a `fetch`: usan servicios (`incidentesService`, `catalogosService`, etc.) a través de hooks de TanStack Query.

## 4. Manejo de estado

| Tipo de estado | Herramienta | Justificación |
|---|---|---|
| Datos del servidor | TanStack Query | Caché por clave, deduplicación, cancelación con `AbortSignal`, estados `isPending`/`isError` listos para la interfaz e invalidación tras cada mutación. No reintenta errores 4xx (un 429 empeoraría). |
| Sesión | Contexto de React (`AuthProvider`) | Estado global pequeño y de baja frecuencia de cambio; no justifica una librería adicional. |
| Filtros y página | Parámetros de la URL | Se pueden compartir, recargar y recorrer con Atrás/Adelante. |
| Formularios | React Hook Form + Zod | Validación declarativa, foco automático en el primer error y errores del servidor asignados al campo correspondiente. |

Al cerrar sesión se vacía la caché de TanStack Query, para que otro usuario en el mismo navegador no vea datos del anterior.

## 5. Autenticación y almacenamiento del token

**Flujo.** El login envía las credenciales, recibe el JWT y su duración (`expiresIn`), guarda el token y pide `/api/v1/usuarios/me` para conocer nombre y rol desde la API, que es la fuente de verdad. Al recargar la página, el rol se vuelve a pedir. Se programa el cierre de sesión para el momento exacto en que vence el JWT y cualquier 401 posterior también cierra la sesión y lleva al login con el mensaje «Tu sesión expiró». El regreso a la ruta pedida solo acepta rutas internas, para evitar redirecciones abiertas. El refresh token, opcional en la pauta, no se usa en el cliente.

**Estrategia de almacenamiento: `sessionStorage`.**

| Opción | Ventaja | Riesgo | Decisión |
|---|---|---|---|
| Solo memoria | Un script inyectado no lo puede leer de un almacenamiento | Se pierde al recargar; obliga a reingresar las credenciales | Descartada por usabilidad |
| `localStorage` | Persiste | Legible por XSS, persiste indefinidamente y se comparte entre pestañas | Descartada |
| `sessionStorage` | Sobrevive a recargas; se borra al cerrar la pestaña; no se comparte entre pestañas | Legible por un script inyectado (XSS) | **Elegida**, con las mitigaciones de abajo |
| Cookie `HttpOnly` + `SameSite` | No legible desde JavaScript | Requiere cambiar la API a autenticación por cookies y agregar protección CSRF | Fuera de alcance; mejora futura |

El riesgo aceptado (lectura por XSS) se mitiga con: renderizado siempre como texto (sin `dangerouslySetInnerHTML`, regla de lint `react/no-danger`), Content-Security-Policy estricta en el build (`script-src 'self'`, sin scripts inline), JWT de 60 minutos, cierre de sesión que borra el token y la caché, y el token nunca en URLs ni en la consola.

## 6. Autorización por roles

`src/features/auth/permisos.ts` replica la matriz RBAC de la API y decide qué se muestra:

| Acción | Administrador | Supervisor | Analista | Cliente |
|---|---|---|---|---|
| Ver incidentes | Todos | Todos | Solo asignados | Solo de su organización |
| Crear incidente | Sí | Sí | No | Sí (sobre activos propios) |
| Editar incidente | Sí | Sí | Solo asignados | No |
| Cambiar estado | Sí | Sí | Solo asignados | No |
| Asignar analista | Sí | Sí | No | No |
| Eliminar incidente | Sí | No | No | No |

`ProtectedRoute` redirige al login sin sesión y a `/acceso-denegado` si el rol no corresponde a la ruta. Si la API responde 403 (por ejemplo, un id de otra organización escrito en la URL) se muestra la misma vista de acceso denegado. La interfaz no reemplaza al backend: la API valida el rol en cada endpoint y la pertenencia de cada objeto (`ValidaAccesoIncidente`), y las pruebas lo demuestran con respuestas 403 reales.

## 7. Seguridad: matriz de riesgos mitigados

| Riesgo | Referencia OWASP | Lado | Dónde aparecería | Mitigación aplicada | Evidencia |
|---|---|---|---|---|---|
| Cross-Site Scripting | Top 10 web A03:2021 Injection | Cliente | Título y descripción de incidentes mostrados en listado y detalle | Todo se renderiza como texto con JSX; prohibido `dangerouslySetInnerHTML` (lint); CSP `script-src 'self'` sin inline en el build | E2E crea un incidente con `<script>` en el título: se muestra literal y no se ejecuta; prueba de componente con `<img onerror>`; `seguridad-api.txt` |
| Robo o filtración del token | API2:2023 Broken Authentication | Cliente | Almacenamiento del JWT en el navegador | `sessionStorage` justificado; JWT de 60 min; expiración programada; logout que borra token y caché; token fuera de URL y consola | `tokenStorage.ts`; pruebas de 401 y cierre de sesión |
| Enumeración de cuentas y fuerza bruta | API2:2023 / API4:2023 | API | Login | Mismo 401 para email inexistente, clave incorrecta o cuenta bloqueada; bloqueo tras 5 fallos; 5 intentos/min por IP con 429 y `Retry-After`; el cliente muestra el mensaje | `SeguridadApiTests`; `seguridad-api.txt` (estados `401,429`); prueba del 429 en el login |
| Control de acceso a nivel de función | API5:2023 BFLA | Ambos | Eliminar, editar, asignar, registrar usuarios | Rutas y acciones condicionadas por rol en la interfaz; `RequireRole` en cada endpoint de escritura | Cliente recibe 403 al eliminar, editar o registrar usuarios; E2E de acceso denegado |
| Acceso a objetos de otros usuarios | API1:2023 BOLA | API | `GET /incidentes/{id}` con un id ajeno | El servidor valida la organización (Cliente) o la asignación (Analista); el incidente toma la organización del activo, nunca del body | `Cliente_NoPuedeVerIncidenteDeOtraOrganizacion`; `seguridad-api.txt` |
| Exposición excesiva de datos | API3:2023 BOPLA | Ambos | Respuestas de incidentes, activos y organizaciones | DTOs proyectados con `.Select()` (sin `password_hash`); el cliente valida el contrato y vacía la caché al cerrar sesión | `seguridad-api.txt` (lista de campos); prueba de contrato inválido |
| Consumo inseguro de respuestas | API10:2023 | Cliente | Cualquier respuesta de la API | Validación de esquema con Zod antes de usar los datos; renderizado como texto | Prueba «rechaza una respuesta que no cumple el contrato» |
| Validación de entradas insuficiente | Top 10 web A03:2021 Injection | Ambos | Formularios y búsqueda | Zod en el cliente; validación obligatoria en el servidor (400 por campo); EF Core parametriza las consultas | Pruebas de validación cliente y servidor; búsqueda con `' OR 1=1` sin resultados |
| Configuración insegura | API8:2023 | API | CORS, cabeceras y errores | CORS con orígenes, métodos y cabeceras explícitos; `nosniff`, `X-Frame-Options`, CSP, `Referrer-Policy`, `no-store`; HSTS fuera de desarrollo; errores genéricos con `traceId` | E2E de CORS bloqueado por el navegador; `SeguridadApiTests`; `seguridad-api.txt` |
| Dependencias vulnerables | Top 10 web A06:2021 | Ambos | `package-lock.json` y paquetes NuGet | `npm audit` sin hallazgos; lockfile versionado; dependencia NuGet transitiva vulnerable fijada a una versión parchada | `npm-audit.txt`, `dotnet-vulnerables.txt` |
| Secretos expuestos | Top 10 web A05:2021 | Cliente | Variables `VITE_*` y configuración de la API | Solo la URL pública de la API en `VITE_*`; `.env` fuera de git; secretos de la API en User Secrets | `.env.example`, `.gitignore`, `SentryGuard/.env.example` |
| CSRF | Top 10 web A01:2021 | Ambos | Peticiones de escritura | No aplica: el JWT viaja en la cabecera `Authorization` (el navegador no lo adjunta solo) y se usa `credentials: 'omit'` | `httpClient.ts` |
| Inventario de API | API9:2023 | API | Documentación y versiones | Scalar y OpenAPI solo en Development; todas las rutas bajo `/api/v1/` | `Program.cs` |

## 8. Seguridad del backend para el consumo desde el navegador

- **CORS** restringido a `https://localhost:5173` (desarrollo) y `https://localhost:4173` (vista previa del build), con métodos y cabeceras explícitos.
- **Cabeceras:** `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` y, en `/api`, `Content-Security-Policy: default-src 'none'` y `Cache-Control: no-store`; HSTS fuera de Development. El frontend en build agrega su propia CSP.
- **Rate limiting** de 5 intentos por minuto por IP en el login y 10 por minuto en la creación de incidentes, con 429 en JSON y `Retry-After`.
- **Errores** sin detalles internos: un JSON con código, mensaje y `traceId`; la traza queda solo en el log.
- **HTTPS** en la API y en el frontend con el certificado de desarrollo de ASP.NET Core, exportado a PEM para Vite (`npm run certs`).

## 9. Accesibilidad (WCAG 2.2 AA)

- HTML semántico: `header`, `nav`, `main`, `footer`, `search`; un `h1` por vista y jerarquía de encabezados; botones y enlaces según su función; tabla con encabezados de columna y fila.
- Formularios: cada control con `label`, ayuda y error vinculados por `aria-describedby`, `aria-invalid`, obligatoriedad indicada con texto, foco en el primer campo con error y resumen anunciado con `role="alert"`.
- Mensajes de estado (WCAG 4.1.3): regiones `aria-live` permanentes para éxitos y errores, y conteo de resultados.
- Teclado: enlace «Saltar al contenido principal», foco visible de 3 px con contraste alto, foco movido al `h1` en cada cambio de ruta y título de pestaña actualizado; diálogo nativo con `showModal()` que atrapa el foco, cierra con Escape y devuelve el foco.
- Contraste ≥ 4.5:1 en todo el texto (pares documentados en `global.css`); severidad y estado con texto e icono, no solo color; `lang="es"`; sin imágenes informativas (el logo es decorativo y está oculto para lectores de pantalla).
- Verificación: axe con etiquetas WCAG 2.0–2.2 A/AA sin violaciones en cinco vistas, oxlint con reglas `jsx-a11y` sin errores y revisión manual con teclado. La revisión con NVDA quedó como lista guiada en `docs/evidencias/revision-manual-accesibilidad.md` para completarse.

## 10. Diseño responsivo

Mobile-first con unidades `rem`, Flexbox y Grid. Bajo 1024 px el listado se presenta como tarjetas (una columna en móvil y dos en tableta) y desde 1024 px como tabla. La cabecera, los filtros y las acciones se reorganizan con `flex-wrap`. Todos los controles miden al menos 44 × 44 px. No hay scroll horizontal a 320, 390, 768 ni 1280 px (comprobado en las E2E), y con zoom al 200 % la vista pasa a tarjetas sin perder contenido. Capturas en `docs/evidencias/capturas/`.

## 11. Pruebas

| Suite | Herramientas | Cobertura | Resultado |
|---|---|---|---|
| Componentes | Vitest, Testing Library, MSW (API simulada; cualquier petición sin handler falla) | Login (éxito, validación, 401, 429), rutas protegidas (sin sesión, retorno, rol insuficiente, 403, 401), CRUD (listar, vacío, error, contrato inválido, crear, validación cliente y servidor, eliminar con confirmación, XSS) | 19/19 |
| End-to-end | Playwright contra la API real y axe | Login → crear → listar → edición denegada; redirección sin sesión; accesibilidad, reflujo y teclado; CORS en el navegador | 5/5 |
| Integración de la API | xUnit, `WebApplicationFactory`, SQL Server de prueba | Suite del Trabajo 1 más validación, errores, enumeración, CORS, cabeceras, paginación y búsqueda | 14/14 |
| Seguridad en ejecución | `scripts/verificar-seguridad.mjs` | 19 controles: 401, 403 por rol y por objeto, 400, errores, inyección, CORS, cabeceras, paginación, 429 | 19/19 |

## 12. Uso de IA

Se usó Claude Code (Claude Sonnet 5 para el Trabajo 1 y Claude Opus 5.5 para el Trabajo 2), además de GitHub Copilot y Claude en claude.ai en el inicio del Trabajo 1. La declaración de herramienta, modelo y plan, los prompts y lo aceptado o corregido en cada parte están en `AI_LOG.md`.

## 13. Limitaciones y trabajo futuro

- Migrar la autenticación a cookie `HttpOnly` + `SameSite` con token anti-CSRF, para que el token no sea legible desde JavaScript.
- Usar el refresh token para renovar la sesión sin pedir credenciales.
- Rate limiting en memoria: para varias instancias de la API se necesitaría un almacén compartido.
- El bundle pesa unos 500 kB; se puede dividir por ruta con carga diferida.
- Completar la revisión con NVDA (lista guiada en las evidencias).
