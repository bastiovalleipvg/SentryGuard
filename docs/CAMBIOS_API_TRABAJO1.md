# Ajustes a la API del Trabajo 1

La pauta del Trabajo 2 pide corregir y documentar las fallas de la API que impidieran o
debilitaran su consumo desde el navegador. Estos son los cambios hechos, con su motivo y el
commit donde se aplicaron.

| # | Cambio | Motivo | Commit |
|---|---|---|---|
| 1 | `GlobalExceptionHandlingMiddleware` responde siempre un JSON genérico (`codigo`, `mensaje`, `traceId`); el detalle de la excepción solo va al log. `BadHttpRequestException` (JSON mal formado) pasa a 400. | Antes devolvía `exception.Message` al cliente, incluidos mensajes internos de EF Core (OWASP API8). JSON mal formado terminaba en 500. | `89a649d` |
| 2 | Login con un único 401 genérico para email inexistente, contraseña incorrecta o cuenta bloqueada; validación de entrada (400). `expiresIn` se calcula desde `Jwt:ExpireMinutes`. | Antes respondía «Contraseña incorrecta. Intento 3/5» solo si la cuenta existía: permitía enumerar usuarios (API2). | `89a649d` |
| 3 | Validación de servidor al crear y editar incidentes: largo de título (5–250) y descripción (10–4000), existencia de severidad y activo. Responde `ValidationProblem` (400) con errores por campo. | Antes aceptaba títulos vacíos y ids inexistentes (terminaban en errores de BD). El frontend necesita errores por campo para mostrarlos (pauta: 400/422). | `89a649d` |
| 4 | `PUT /api/v1/incidentes/{id}` acepta también `severidadId`. | Permitir editar la severidad desde el formulario de edición. | `89a649d` |
| 5 | Búsqueda (`q`) y filtros (`estadoId`, `severidadId`) en `GET /api/v1/incidentes`. | La pauta pide listado con búsqueda o filtros. La búsqueda va parametrizada por EF Core. | `89a649d` |
| 6 | `pageSize` acotado a 1–50 en todos los listados (`Paginacion.Normalizar`). | Un `pageSize` arbitrario permitía descargar una tabla completa en una request (API4); `pageSize=0` provocaba un 500. | `89a649d` |
| 7 | `IncidenteDTO` incluye `severidadId`, `estadoId`, `activoId`, `organizacionId` y `analistaAsignadoId`. | El formulario de edición y los permisos del frontend necesitan los ids, no solo los nombres. Ningún campo sensible. | `89a649d` |
| 8 | Nuevo `GET /api/v1/usuarios/analistas` (solo Administrador y Supervisor) con `id` y `nombre`. `GET /me` incluye `organizacionId`. | Selector para asignar analistas desde la interfaz sin exponer el resto de los datos de usuarios. | `89a649d` |
| 9 | CORS limitado a `https://localhost:5173` y `https://localhost:4173`, con métodos y cabeceras explícitos; expone `Location` y `Retry-After`. | El origen anterior (`http://localhost:4200`) no correspondía al frontend; `AllowAnyHeader/AllowAnyMethod` era más amplio de lo necesario (API8). | `89a649d` |
| 10 | 429 con cuerpo JSON (`codigo`, `mensaje`, `reintentarEnSegundos`) y cabecera `Retry-After`. | Antes el 429 llegaba vacío; el cliente no podía mostrar un mensaje claro (API4). | `89a649d` |
| 11 | `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`, `Cache-Control: no-store` y `Permissions-Policy` en las respuestas de `/api`. | Endurecer respuestas JSON para el consumo desde navegador (API8). | `89a649d` |
| 12 | `System.Security.Cryptography.Pkcs` fijado en 10.0.13. | `Microsoft.Data.SqlClient` traía la 9.0.11, con vulnerabilidades alta y moderada (GHSA-846f-f764-76mx, GHSA-2fmv-rv2c-59x7). | `89a649d` |
| 13 | Migración `SeedActivosDemo`: tres activos para la organización de demostración. | Sin activos, un Cliente no podía registrar incidentes desde el frontend (solo Administrador y Supervisor crean activos). | `88a6670` |
| 14 | 8 pruebas de integración nuevas (`SeguridadApiTests`) y ejecución en serie de las clases de prueba. | Cubrir los controles anteriores; las dos clases recrean la misma BD de prueba. | `89a649d` |
| 15 | `Endpoints/Validacion.cs`: validación compartida (texto, horas, correo, política de contraseñas, hostname/IP, RUT con dígito verificador) y respuesta 409 con `{ codigo, mensaje }`. | Las altas de activos, organizaciones, usuarios y catálogos aceptaban cualquier valor (nombres vacíos, RUT sin formato, contraseñas de un carácter). | pendiente |
| 16 | Activos: `PUT` edita nombre, hostname/IP y categoría con validación; filtros `q`, `categoriaId` y `organizacionId` (ignorado para Cliente); DTO con `categoriaId`, `organizacionId` y `organizacion`; `DELETE` responde 409 si el activo tiene incidentes. | El `PUT` solo cambiaba el nombre; las FK son `Restrict`, así que borrar un activo con incidentes terminaba en 500. | pendiente |
| 17 | Organizaciones: `PUT` edita razón social, RUT y plan; RUT único con dígito verificador (exigido solo si cambia); filtros `q` y `planId`; `DELETE` 409 si tiene activos, incidentes, usuarios o contratos. | Mismo problema de validación y de 500 al borrar. | pendiente |
| 18 | Contratos SLA: `GET`/`POST /organizaciones/{id}/contratos-sla`, `POST .../{contratoId}/finalizar` y `DELETE`. Valida vigencia, horas (0–720], severidades sin repetir y que los períodos no se superpongan. | `tb_ContratoSLA` y `tb_ContratoSLA_Detalle` existían en el modelo sin ningún endpoint. | pendiente |
| 19 | `GET /incidentes/sla/cumplimiento/{id}` usa las horas del contrato vigente al crear el incidente (si no hay, las de la severidad) y devuelve `fuente`, `fechaLimite` y `cerrado`. | El contrato SLA no tenía efecto sobre el cálculo. | pendiente |
| 20 | Bitácora y evidencias: `GET` devuelve lista vacía (200) en vez de 404; validación al agregar; la evidencia solo acepta `https://` o una ruta relativa. Crear, editar, cambiar estado y asignar registran entradas automáticas. Volver a un estado abierto borra `FechaCierre`. Filtro `activoId` en el listado de incidentes. | El 404 se confundía con «incidente inexistente»; una ruta `javascript:` podía terminar en un enlace (XSS); la bitácora solo tenía lo que alguien anotara a mano. | pendiente |
| 21 | Usuarios: `GET /usuarios` (Administrador, con filtros) y `PUT /usuarios/{id}` (nombre, rol, organización, activo; no se puede desactivar ni cambiar el rol de la propia cuenta). El registro valida correo único, contraseña y que solo el Cliente tenga organización. | No había forma de administrar cuentas; un usuario se «elimina» desactivándolo para conservar su bitácora. | pendiente |
| 22 | Catálogos: validación con nombre único; `PUT`/`DELETE` de severidades, categorías y planes (409 si están en uso); orden estable en los listados. Los estados solo se crean. | Los `POST` aceptaban valores vacíos o duplicados; no había edición. | pendiente |
| 23 | Nuevo `GET /api/v1/auditoria` (solo Administrador) sobre `tb_LogAuditoria`. | La tabla se llenaba en cada escritura, pero no se podía consultar. | pendiente |
| 24 | 9 pruebas de integración nuevas (`MantenedoresApiTests`). | Cubrir 400, 403, 409 y el aislamiento por organización de los recursos nuevos. | pendiente |

Además, el estudiante ya había sacado la clave JWT y la cadena de conexión de
`appsettings.json`, moviéndolas a User Secrets y rotando la clave (`57ec111`, `93e86aa`).

La suite de integración del Trabajo 1 sigue pasando completa (23/23 con las pruebas nuevas, ver
`docs/evidencias/pruebas-backend.txt`).
