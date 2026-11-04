# 2. Requerimientos e historias de usuario

**Priorización (MoSCoW):** **M** = debe tenerse (obligatorio en la pauta), **S** = debería tenerse, **C** = podría tenerse.

**Estado:** ✅ implementado · 🟡 parcial · 📋 planificado (con el hito en que se hace).

## 2.1 Requerimientos funcionales

| ID | Requerimiento | Prioridad | Estado | Cómo se verifica |
|---|---|---|---|---|
| RF-01 | Iniciar sesión con correo y contraseña; recibir un JWT de 60 minutos | M | ✅ | `POST /api/v1/usuarios/login`; prueba `LoginPage.test.tsx` |
| RF-02 | Cerrar sesión, y cerrarla automáticamente al expirar el token o recibir un 401 | M | ✅ | `AuthContext.tsx`; prueba `ProtectedRoute.test.tsx` |
| RF-03 | Mostrar menús y acciones según el rol (4 roles) | M | ✅ | `permisos.ts`, `Layout.tsx`; prueba `mantenedores.test.tsx` |
| RF-04 | Registrar un incidente sobre un activo, con título, descripción y severidad | M | ✅ | `POST /api/v1/incidentes` |
| RF-05 | Listar incidentes con búsqueda, filtros (estado, severidad, activo) y paginación | M | ✅ | `GET /api/v1/incidentes` |
| RF-06 | **Ordenar** los listados por columna (fecha, severidad, estado, título) | M | 📋 Hito 2 | Parámetro `orden` en las APIs de listado y encabezados ordenables en la interfaz |
| RF-07 | Ver, editar y eliminar un incidente (eliminar pide confirmación) | M | ✅ | `IncidenteDetailPage.tsx` |
| RF-08 | Asignar un incidente a un analista | M | ✅ | `POST /api/v1/incidentes/{id}/asignar` |
| RF-09 | Cambiar el estado **solo por transiciones válidas** del flujo | M | 🟡 Hito 2 | Hoy acepta cualquier estado; se agrega `tb_TransicionEstado` y la API responde 409 ante una transición inválida |
| RF-10 | Registrar entradas de bitácora y evidencias; las acciones del flujo se registran solas | M | ✅ | `BitacoraPanel.tsx`, `EvidenciasPanel.tsx` |
| RF-11 | Calcular el cumplimiento del SLA con el contrato vigente o, si no hay, con la severidad | M | ✅ | `GET /api/v1/incidentes/sla/cumplimiento/{id}` |
| RF-12 | Administrar activos con filtros por nombre/IP, categoría y organización | M | ✅ | `ActivosListPage.tsx` |
| RF-13 | Administrar organizaciones (RUT con dígito verificador) y sus contratos SLA | M | ✅ | `OrganizacionAPI.cs` |
| RF-14 | Administrar usuarios: crear, cambiar el rol, desactivar | M | ✅ | `UsuariosPages.tsx` |
| RF-15 | Administrar catálogos (severidades, categorías de activo y de incidente, planes y estados) | S | 🟡 Hito 2 | La categoría de incidente es nueva (`tb_CategoriaIncidente`) |
| RF-16 | **Auditoría por entidad**: quién creó o modificó qué registro, con los valores anteriores y nuevos, y cuándo | M | 🟡 Hito 2 | Hoy se registra el método, la ruta, el usuario y la IP; se agregan la entidad, el id y el detalle del cambio |
| RF-17 | **Dashboard** con al menos 6 indicadores y 4 gráficos, filtros por fecha, organización, severidad y categoría | M | 📋 Hito 4 | `GET /api/v1/dashboard/*` y la pantalla `/dashboard` |
| RF-18 | **Reporte operativo**: detalle de incidentes con los filtros aplicados | M | 📋 Hito 4 | `GET /api/v1/reportes/operativo` |
| RF-19 | **Reporte gerencial**: totales por organización, severidad y estado, con gráficos | M | 📋 Hito 4 | `GET /api/v1/reportes/gerencial` |
| RF-20 | **Reporte parametrizable por rango de fechas**: cumplimiento del SLA y tiempo de resolución por mes | M | 📋 Hito 4 | `GET /api/v1/reportes/sla?desde&hasta` |
| RF-21 | Exportar cada reporte a **Excel, PDF y CSV** con los filtros elegidos, respetando el rol y registrando quién exportó | M | 📋 Hito 4 | `GET /api/v1/reportes/{tipo}/exportar?formato=` |
| RF-22 | Imprimir cada reporte desde el navegador sin menús ni botones | M | 📋 Hito 4 | Estilos `@media print` |
| RF-23 | **IA: sugerir la severidad y la categoría** de un incidente a partir de su descripción; la persona la acepta o la cambia | M | 📋 Hito 3 | `POST /api/v1/ia/clasificar-incidente` |
| RF-24 | **IA: resumir en lenguaje natural** el dashboard o un reporte con los filtros aplicados | S | 📋 Hito 3 | `POST /api/v1/ia/resumir` |
| RF-25 | Generar datos sintéticos: 500 o más incidentes repartidos en 12 meses | M | 📋 Hito 2 | Generador con semilla fija, idempotente |

## 2.2 Requerimientos no funcionales

| ID | Atributo | Requerimiento verificable | Estado | Cómo lo atiende la arquitectura |
|---|---|---|---|---|
| RNF-01 | Rendimiento | Con 500 o más incidentes, un listado paginado responde en menos de 500 ms (p95) y el dashboard en menos de 1,5 s | 📋 medir en el Hito 4 | Paginación en el servidor (máx. 50 por página), índices en las FK y en `fecha_creacion`, agregaciones en SQL (vistas) en lugar de en memoria |
| RNF-02 | Seguridad | JWT HS256 de 60 min; contraseñas con `PasswordHasher` (PBKDF2); bloqueo de la cuenta tras 5 intentos durante 15 min; 5 intentos de login por minuto por IP; ningún secreto en el repositorio | ✅ | `AuthService.cs`, `Program.cs` (rate limiting), User Secrets y variables de entorno |
| RNF-03 | Seguridad | Mitigar al menos 5 riesgos del OWASP API Security Top 10 | ✅ (13 documentados) | Ver `docs/INFORME_TECNICO.md` §7 y `docs/evidencias/seguridad-api.txt` |
| RNF-04 | Disponibilidad | Los contenedores se reinician solos si fallan, y la API expone un *health check* | 📋 Hito 3 | `restart: unless-stopped` y `healthcheck` en compose; endpoint `/health` |
| RNF-05 | Recuperación | Respaldo diario de la base; restauración en menos de 30 min (RTO) con una pérdida máxima de 24 h (RPO) | 📋 Hito 2 | Script de `BACKUP DATABASE` en un volumen aparte; restauración probada y documentada |
| RNF-06 | Accesibilidad | WCAG 2.2 AA en todas las pantallas: axe sin violaciones y un flujo completo probado con teclado y NVDA | 🟡 (axe ✅ en 15 vistas, NVDA pendiente) | HTML semántico, `FormField`, foco gestionado, `ConfirmDialog` nativo |
| RNF-07 | Usabilidad | Sin desplazamiento horizontal entre 360 y 1440 px; objetivos táctiles de 44 × 44 px | 🟡 (verificado de 320 a 1280) | Mobile first, `ListadoResponsivo` (tabla o tarjetas), menú plegable |
| RNF-08 | Mantenibilidad | Lógica de negocio en servicios con interfaz y pruebas unitarias; endpoints delgados | 📋 Hito 2 | Capa `Services/` con inyección de dependencias (ver arquitectura) |
| RNF-09 | Portabilidad | El sistema completo arranca con `docker compose up` en un equipo con Docker | 📋 Hito 3 | Dockerfiles multi-stage y `compose.yaml` |
| RNF-10 | Integración | Las llamadas a la IA tienen un tiempo máximo de 15 s; si la IA falla, la API responde 502/504 con el formato de error estándar y el formulario sigue funcionando sin la sugerencia | 📋 Hito 3 | Adaptador `IClasificadorIncidentes` con `HttpClient` tipado y *timeout* |
| RNF-11 | Auditabilidad | Toda creación, modificación, eliminación y exportación queda registrada con el usuario, la entidad y la fecha | 🟡 Hito 2 | Interceptor de `SaveChanges` en EF Core y registro explícito de las exportaciones |
| RNF-12 | Calidad | Pipeline CI que compila, ejecuta todas las pruebas y publica la imagen solo si pasan | 📋 Hito 3 | GitHub Actions |

## 2.3 Historias de usuario

Formato: *Como [rol], quiero [acción] para [beneficio]*. Los criterios de aceptación usan *Dado, Cuando, Entonces*.

### Cliente

**HU-01 · Reportar un incidente** (M, ✅)
Como Cliente, quiero reportar un incidente sobre un activo de mi organización para que CyberOps lo atienda.
- Dado que inicié sesión como Cliente, cuando completo el título (5 a 250 caracteres), la descripción, el activo y la severidad, y envío el formulario, entonces el incidente queda Abierto y aparece en mi listado.
- Dado que el título tiene menos de 5 caracteres, cuando envío el formulario, entonces el campo muestra el error y no se envía nada a la API.
- Dado un activo de otra organización, cuando intento usarlo (incluso forzando la petición), entonces la API responde 403.

**HU-02 · Recibir una sugerencia de clasificación** (M, 📋 Hito 3)
Como Cliente, quiero que el sistema me sugiera la severidad y la categoría a partir de mi descripción para no tener que conocer la clasificación técnica.
- Dado que escribí una descripción de al menos 20 caracteres, cuando pido la sugerencia, entonces veo la severidad, la categoría y una justificación breve, y los campos se llenan solo si los acepto.
- Dado que el servicio de IA no responde en 15 s, cuando pido la sugerencia, entonces veo un aviso claro y puedo completar los campos a mano.

**HU-03 · Seguir mis incidentes** (M, ✅)
Como Cliente, quiero ver el estado, la bitácora y el SLA de mis incidentes para saber cómo avanza la atención.
- Dado un incidente de mi organización, cuando lo abro, entonces veo su estado, la bitácora y el plazo del SLA, pero no los botones para editar, cambiar el estado ni registrar entradas.
- Dado un incidente de otra organización, cuando escribo su URL, entonces veo «Acceso denegado» y la API responde 403.

**HU-04 · Exportar el reporte de mi organización** (S, 📋 Hito 4)
Como Cliente, quiero exportar el reporte operativo de mis incidentes para compartirlo en mi empresa.
- Dado que filtro por un rango de fechas, cuando exporto a Excel, PDF o CSV, entonces el archivo contiene solo los incidentes de mi organización con esos filtros.

### Analista

**HU-05 · Ver lo que tengo asignado** (M, ✅)
Como Analista, quiero ver solo los incidentes que tengo asignados y sus activos para concentrarme en mi trabajo.
- Dado que inicié sesión como Analista, cuando abro Incidentes o Activos, entonces solo veo lo asignado a mí, y el menú no muestra Organizaciones, Usuarios, Catálogos ni Auditoría.

**HU-06 · Documentar la atención** (M, ✅)
Como Analista, quiero registrar entradas en la bitácora y evidencias en mis incidentes para dejar trazabilidad de lo que hice.
- Dado un incidente asignado a mí, cuando agrego una entrada con la acción (3 a 100 caracteres), entonces aparece primero en el historial con mi nombre y la hora.
- Dado que registro una evidencia con una ubicación `javascript:…`, cuando la envío, entonces el formulario la rechaza y la API también (400).
- Dado un incidente asignado a mí, cuando lo abro, entonces no veo «Editar» ni «Gestión», y la API responde 403 si intento editarlo o cambiar su estado.

### Supervisor

**HU-07 · Asignar y mover incidentes** (M, 🟡)
Como Supervisor, quiero asignar incidentes y moverlos por su flujo para coordinar al equipo.
- Dado un incidente Abierto, cuando lo asigno a un analista, entonces la bitácora registra la asignación y el incidente aparece en la lista de ese analista.
- Dado un incidente Abierto, cuando intento pasarlo directo a Cerrado, entonces la API responde 409 y la interfaz solo ofrece los estados permitidos *(Hito 2)*.

**HU-08 · Analizar el dashboard** (M, 📋 Hito 4)
Como Supervisor, quiero ver indicadores y gráficos filtrables para detectar a tiempo los SLA en riesgo y la carga del equipo.
- Dado que filtro por los últimos 3 meses y una organización, cuando cambio el filtro, entonces los 6 indicadores y los 4 gráficos se recalculan con datos reales del sistema.
- Dado que uso un lector de pantalla, cuando llego a un gráfico, entonces escucho su título y su descripción, y puedo abrir una tabla con sus datos.

**HU-09 · Generar reportes** (M, 📋 Hito 4)
Como Supervisor, quiero generar el reporte gerencial de un período para presentarlo a la gerencia.
- Dado un rango de fechas, cuando exporto a PDF, entonces el archivo tiene título, fecha de emisión, filtros usados, numeración de páginas y texto seleccionable.
- Dado que exporto cualquier reporte, cuando reviso la auditoría, entonces aparece mi usuario, el reporte, los filtros y el formato.
- Dado que uso «Imprimir», entonces la vista de impresión no muestra menús ni botones.

**HU-10 · Resumen con IA** (S, 📋 Hito 3)
Como Supervisor, quiero un resumen en lenguaje natural del dashboard filtrado para preparar mi reporte semanal más rápido.
- Dado el dashboard filtrado, cuando pido el resumen, entonces recibo un texto breve que menciona solo cifras presentes en los datos enviados, marcado como «Generado con IA».

### Administrador

**HU-11 · Administrar usuarios** (M, ✅)
Como Administrador, quiero crear usuarios, asignarles un rol y desactivarlos para controlar el acceso.
- Dada una contraseña débil, cuando creo el usuario, entonces el formulario muestra los requisitos y la API también la rechaza (400).
- Dada mi propia cuenta, cuando la edito, entonces no puedo cambiar mi rol ni desactivarme.

**HU-12 · Administrar organizaciones y contratos SLA** (M, ✅)
Como Administrador, quiero registrar organizaciones y sus contratos SLA para que el plazo de cada incidente sea el pactado.
- Dado un RUT con un dígito verificador inválido, cuando guardo, entonces se rechaza en el formulario y en la API.
- Dado un contrato que se superpone con otro vigente, cuando lo registro, entonces la API responde 400 indicando la superposición.

**HU-13 · Auditar los cambios** (M, 🟡 Hito 2)
Como Administrador, quiero ver quién creó o modificó cada registro y qué cambió para investigar problemas.
- Dado que un Supervisor cambió la severidad de un incidente, cuando abro la auditoría y filtro por ese incidente, entonces veo el usuario, la fecha, el campo, el valor anterior y el nuevo.

## 2.4 Matriz de permisos

Es la que aplica hoy la API (`RequireRole` y `AuthService.ValidaAcceso*`); el frontend la refleja en `permisos.ts`.

| Acción | Administrador | Supervisor | Analista | Cliente |
|---|---|---|---|---|
| Ver incidentes | Todos | Todos | Solo asignados | Solo de su organización |
| Crear incidente | Sí | Sí | No | Sí |
| Editar y cambiar estado | Sí | Sí | No | No |
| Asignar analista | Sí | Sí | No | No |
| Bitácora y evidencias | Sí | Sí | Solo en sus asignados | Solo ve |
| Eliminar incidente | Sí | No | No | No |
| Activos | Todos (crea, edita, elimina) | Todos (crea, edita) | Solo los de sus incidentes | Solo los de su organización |
| Organizaciones y contratos SLA | Gestiona | Ve | No | Solo la suya |
| Usuarios, catálogos y auditoría | Sí | No | No | No |
| Dashboard *(Hito 4)* | Todo | Todo | Solo sus asignados | Solo su organización |
| Reportes y exportación *(Hito 4)* | Todos | Todos | No | Solo su organización |
| IA: clasificar *(Hito 3)* | Sí | Sí | No | Sí |
| IA: resumir *(Hito 3)* | Sí | Sí | No | No |

## 2.5 Priorización y orden de trabajo

1. **Hito 2 (datos y backend):** RF-06, RF-09, RF-15, RF-16 y RF-25, más RNF-05 y RNF-08. Son la base de todo lo demás (el dashboard necesita los datos, y la IA necesita la categoría de incidente).
2. **Hito 3 (front, contenedores e IA):** RF-23 y RF-24, más RNF-04, RNF-09, RNF-10 y RNF-12.
3. **Hito 4 (análisis y reportes):** RF-17 a RF-22, y la medición de RNF-01.
