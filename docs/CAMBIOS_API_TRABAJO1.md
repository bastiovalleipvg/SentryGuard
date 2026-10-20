# Ajustes a la API del Trabajo 1

La pauta del Trabajo 2 pide corregir y documentar las fallas de la API que impidieran o
debilitaran su consumo desde el navegador. Estos son los cambios hechos, con su motivo y el
commit donde se aplicaron.

| # | Cambio | Motivo | Commit |
|---|---|---|---|
| 1 | `GlobalExceptionHandlingMiddleware` responde siempre un JSON genérico (`codigo`, `mensaje`, `traceId`); el detalle de la excepción solo va al log. `BadHttpRequestException` (JSON mal formado) pasa a 400. | Antes devolvía `exception.Message` al cliente, incluidos mensajes internos de EF Core (OWASP API8). JSON mal formado terminaba en 500. | `48dbc08` |
| 2 | Login con un único 401 genérico para email inexistente, contraseña incorrecta o cuenta bloqueada; validación de entrada (400). `expiresIn` se calcula desde `Jwt:ExpireMinutes`. | Antes respondía «Contraseña incorrecta. Intento 3/5» solo si la cuenta existía: permitía enumerar usuarios (API2). | `48dbc08` |
| 3 | Validación de servidor al crear y editar incidentes: largo de título (5–250) y descripción (10–4000), existencia de severidad y activo. Responde `ValidationProblem` (400) con errores por campo. | Antes aceptaba títulos vacíos y ids inexistentes (terminaban en errores de BD). El frontend necesita errores por campo para mostrarlos (pauta: 400/422). | `48dbc08` |
| 4 | `PUT /api/v1/incidentes/{id}` acepta también `severidadId`. | Permitir editar la severidad desde el formulario de edición. | `48dbc08` |
| 5 | Búsqueda (`q`) y filtros (`estadoId`, `severidadId`) en `GET /api/v1/incidentes`. | La pauta pide listado con búsqueda o filtros. La búsqueda va parametrizada por EF Core. | `48dbc08` |
| 6 | `pageSize` acotado a 1–50 en todos los listados (`Paginacion.Normalizar`). | Un `pageSize` arbitrario permitía descargar una tabla completa en una request (API4); `pageSize=0` provocaba un 500. | `48dbc08` |
| 7 | `IncidenteDTO` incluye `severidadId`, `estadoId`, `activoId`, `organizacionId` y `analistaAsignadoId`. | El formulario de edición y los permisos del frontend necesitan los ids, no solo los nombres. Ningún campo sensible. | `48dbc08` |
| 8 | Nuevo `GET /api/v1/usuarios/analistas` (solo Administrador y Supervisor) con `id` y `nombre`. `GET /me` incluye `organizacionId`. | Selector para asignar analistas desde la interfaz sin exponer el resto de los datos de usuarios. | `48dbc08` |
| 9 | CORS limitado a `https://localhost:5173` y `https://localhost:4173`, con métodos y cabeceras explícitos; expone `Location` y `Retry-After`. | El origen anterior (`http://localhost:4200`) no correspondía al frontend; `AllowAnyHeader/AllowAnyMethod` era más amplio de lo necesario (API8). | `48dbc08` |
| 10 | 429 con cuerpo JSON (`codigo`, `mensaje`, `reintentarEnSegundos`) y cabecera `Retry-After`. | Antes el 429 llegaba vacío; el cliente no podía mostrar un mensaje claro (API4). | `48dbc08` |
| 11 | `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`, `Cache-Control: no-store` y `Permissions-Policy` en las respuestas de `/api`. | Endurecer respuestas JSON para el consumo desde navegador (API8). | `48dbc08` |
| 12 | `System.Security.Cryptography.Pkcs` fijado en 10.0.13. | `Microsoft.Data.SqlClient` traía la 9.0.11, con vulnerabilidades alta y moderada (GHSA-846f-f764-76mx, GHSA-2fmv-rv2c-59x7). | `48dbc08` |
| 13 | Migración `SeedActivosDemo`: tres activos para la organización de demostración. | Sin activos, un Cliente no podía registrar incidentes desde el frontend (solo Administrador y Supervisor crean activos). | `4cf8c40` |
| 14 | 8 pruebas de integración nuevas (`SeguridadApiTests`) y ejecución en serie de las clases de prueba. | Cubrir los controles anteriores; las dos clases recrean la misma BD de prueba. | `48dbc08` |

Además, el estudiante ya había sacado la clave JWT y la cadena de conexión de
`appsettings.json`, moviéndolas a User Secrets y rotando la clave (`29ead3a`, `35c57b5`).

La suite de integración del Trabajo 1 sigue pasando completa (14/14, ver
`docs/evidencias/pruebas-backend.txt`).
