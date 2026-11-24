# 10. Pendientes para cumplir al 100% hasta el Hito 3

**Fecha:** 13 de noviembre de 2026 · **Estado del código:** rama `main`, 17 commits por delante de `origin/main` (sin subir).
**Equipo:** Bastián Ovalle, Rafael Navarrete (desarrolla estas tareas con IA) y Adriel Troncoso.

Este documento está escrito para que Rafael se lo pase a su asistente de IA tarea por tarea. Cada tarea trae: **por qué** (criterio de la rúbrica), **qué hacer**, **archivos**, **criterio de aceptación** y **cómo probarlo**.

## 0. Contexto que debe leer la IA antes de empezar

Pega esto al inicio de cada conversación con la IA:

> Proyecto: SentryGuard, gestión de incidentes de ciberseguridad (Evaluación 3 de Lenguaje de Programación Web II). Monorepo: `SentryGuard/` (API .NET 10, EF Core 10, SQL Server 2022), `SentryGuard.Tests/` (xUnit), `sentryguard-web/` (React 19 + TypeScript + Vite, TanStack Query, React Hook Form + Zod, Vitest + MSW, Playwright), `docs/evaluacion3/` (documentación), `compose.yaml` (web nginx, api, db, seed), `.github/workflows/ci.yml`.
> Arquitectura: endpoints delgados → servicios (`Services/`, devuelven `ServiceResult<T>`) → repositorios (`Repositories/`) → `SentryGuardDbContext`. Errores con ProblemDetails (RFC 9457) y campo `codigo`. Roles: Administrador, Supervisor, Analista, Cliente. Texto de la interfaz y de los mensajes en español.
> Reglas del equipo: (1) commits con la convención `tipo(alcance): descripción` en español, minúsculas y sin tildes (feat, fix, docs, test, ci, chore, refactor; alcances db, api, backend, ui, docker, seed); (2) un commit por cambio coherente, **nunca** commits llamados "fase X" ni "hito X"; (3) **sin** la línea `Co-Authored-By` de la IA; (4) no subir secretos ni `.env`; (5) todo cambio lleva pruebas y actualiza la documentación de `docs/evaluacion3/` si cambia el contrato, el modelo o la arquitectura; (6) registrar el uso de IA en `AI_LOG.md`.
> Cómo levantar todo: `./scripts/generar-env.ps1` y `docker compose up --build` (app en http://localhost:8088). Pruebas del backend (necesitan SQL Server): con el compose arriba, `ConnectionStrings__DefaultConnection="Server=127.0.0.1,14333;Database=SentryGuardDB_Test;User Id=sa;Password=<SA_PASSWORD de .env>;TrustServerCertificate=True;"` y `dotnet test SentryGuard.slnx`. Frontend: `cd sentryguard-web && npm test -- --run && npm run lint && npm run build`.

## 1. Qué ya está hecho (para no repetirlo)

| Área | Estado |
|---|---|
| Base de datos | Migraciones, categoría de incidente, `tb_TransicionEstado`, auditoría de `tb_Incidente` y `tb_Activo`, 3 vistas, FK compuesta incidente-activo-organización, CHECK de fecha de cierre, 520 incidentes sintéticos, respaldo y restauración probados |
| API | Servicios + repositorios, ProblemDetails, JWT y 4 roles, `orden` en 4 listados, filtro por categoría, transiciones desde la base, IA (clasificar y resumir), `/health`, OpenAPI |
| Pruebas | 73 de 73 en backend (integración y unitarias), 35 de 35 en frontend |
| Contenedores | Dockerfiles multi-stage con versión fija, compose endurecido, arranque con un comando probado desde cero, secretos sin valor por defecto |
| CI | Workflow escrito (**no ejecutado en GitHub todavía**) |
| Frontend | Interfaz por rol conectada a la API, categoría en el formulario, transiciones desde la API, sugerencia de IA en el formulario |

## 2. Resumen de prioridades

| Prioridad | Significado | Tareas |
|---|---|---|
| **P0** | Bloquea decir "Hito 3 cumplido" | A1, A2, A3, D1 |
| **P1** | Puntos de la rúbrica que hoy se pierden | B1–B6, C1–C5, E1–E6 |
| **P2** | Sube de "Logrado" a "Destacado" o reduce riesgo | A4, A5, B7, B8, C6, C7, D2–D5, E7, E8 |

Orden sugerido: **A → D1 → B → C → E → P2**. Las secciones B, C y E se pueden repartir en paralelo (ver §9).

---

## A. Verificar lo construido (P0)

### A1. Subir a GitHub y hacer que el CI pase en verde (P0)
- **Por qué:** el Hito 3 pide "pipeline funcional". El workflow está escrito pero nunca se ejecutó; puede fallar por detalles del runner.
- **Qué hacer:**
  1. Revisar el historial por secretos (ver D1) **antes** de subir.
  2. `git push origin main` (o abrir un PR desde una rama) y mirar la pestaña Actions.
  3. Corregir cada job que falle: `backend` (SQL Server con `docker run`, `ConnectionStrings__DefaultConnection`, `dotnet test`, revisión de paquetes vulnerables), `frontend` (lint, pruebas, build, `npm audit --audit-level=high`), `contenedores` (`docker compose -f compose.yaml up --build -d --wait`, `curl` de `/health`, de `/` y de `/api/v1/catalogos/severidades`, código de salida de `sentryguard-seed`) y `publicar` (login en GHCR con `GITHUB_TOKEN`).
  4. Confirmar que `publicar` **solo** corre en `push` a `main` y solo si los tres jobs anteriores pasaron.
  5. Confirmar en los logs que ninguna clave aparece (se usa `::add-mask::`).
  6. Verificar en GitHub → Packages que las imágenes `api` y `web` existen y, si el repositorio es privado, que el docente puede verlas o que se documenta cómo.
- **Archivos:** `.github/workflows/ci.yml`, `.github/dependabot.yml`.
- **Aceptación:** captura de una ejecución verde de los 4 jobs, guardada en `docs/evidencias/ci-ejecucion.png`; una ejecución en un PR donde `publicar` se omite.
- **Pistas de fallos probables:** la salida localizada de `dotnet list package --vulnerable` (en el runner es inglés: `has the following vulnerable`); `grep -q "Alta"` del JSON de severidades; tiempo de espera del `--wait` en el runner; permisos `packages: write`.

### A2. Prueba en una máquina limpia (P0)
- **Por qué:** la rúbrica exige que el sistema se ejecute "sin pasos que dependan de configuraciones previas no documentadas".
- **Qué hacer:** en un equipo (o máquina virtual) sin el proyecto: `git clone`, `./scripts/generar-env.ps1` (o `.sh`), `docker compose up --build`, abrir http://localhost:8088, iniciar sesión con los 4 usuarios. Probar en Windows y, si se puede, en Linux o macOS con chip ARM (la imagen de SQL Server 2022 requiere emulación x86 en ARM; si no funciona, documentarlo en el README con la alternativa `platform: linux/amd64`).
- **Aceptación:** pasos del README reproducidos sin ayuda; tiempo de la primera subida anotado; problemas encontrados corregidos o documentados.
- **Archivos:** `README.md` (sección "Ejecución con Docker").

### A3. Recorrer la aplicación en un navegador dentro del compose (P0)
- **Por qué:** el contenedor `web` solo se probó con `curl`, no con un navegador. La CSP, las rutas relativas `same-origin` y el proxy pueden romper algo visible.
- **Qué hacer:** con el compose arriba, entrar con cada rol y recorrer: login, listado de incidentes (búsqueda, filtros, paginación), crear incidente con la sugerencia de IA, editar, cambiar estado, asignar analista, bitácora, evidencias, SLA, activos, organizaciones (contratos SLA), usuarios, catálogos, auditoría, cerrar sesión. Abrir la consola del navegador y confirmar que no hay errores de CSP ni 404/CORS.
- **Aceptación:** lista de verificación por rol sin errores; capturas de la pantalla de listado y de detalle en `docs/evidencias/capturas/` con la versión del compose (puerto 8088).
- **Cómo hacerlo con IA:** pedirle a la IA que use la herramienta de navegador (o Playwright) contra `http://localhost:8088`.

### A4. Ejecutar las pruebas E2E contra el compose (P2)
- **Por qué:** `sentryguard-web/playwright.config.ts` apunta a `https://localhost:5173` y `https://localhost:7166` (el modo local), no al compose.
- **Qué hacer:** agregar una configuración o variable (`E2E_BASE_URL`, por defecto la actual) para ejecutar los mismos tests contra `http://localhost:8088`; sin `webServer` cuando se use el compose. Actualizar los helpers que asumen HTTPS y el origen del test de CORS (`e2e/seguridad.spec.ts`). Agregar `npm run test:e2e:docker`.
- **Archivos:** `sentryguard-web/playwright.config.ts`, `sentryguard-web/e2e/*.ts`, `sentryguard-web/package.json`.
- **Aceptación:** los 5 tests existentes pasan contra el compose; documentado en el README.

### A5. Refrescar las evidencias de pruebas (P2)
- **Por qué:** los archivos de `docs/evidencias/` son del Trabajo 2 (por ejemplo `pruebas-backend.txt` dice 26 de octubre) y la rúbrica exige "reporte de ejecución de la suite" actual.
- **Qué hacer:** regenerar `pruebas-backend.txt` (73 pruebas), `pruebas-componentes.txt`, `pruebas-e2e.txt`, `axe-reporte.json`, `npm-audit.txt`, `dotnet-vulnerables.txt` y `seguridad-api.txt` (ver B5), cada uno con fecha, comando y resultado.
- **Aceptación:** cada archivo trae el comando y la fecha de hoy.

---

## B. Backend y base de datos

### B1. Terminar las restricciones CHECK de la base (P1)
- **Por qué:** criterio 2 (restricciones de integridad) y requisito "validación en tres niveles". Hoy solo existen `CK_tb_Incidente_FechaCierre` y `CK_tb_TransicionEstado_OrigenDistintoDestino`.
- **Qué hacer:** una migración nueva (`dotnet ef migrations add RestriccionesCheck`) que agregue, con nombres `CK_tabla_regla`:
  - `tb_Severidad`: `horas_sla > 0 AND horas_sla <= 720`.
  - `tb_PlanServicio`: `tiempo_respuesta_base_h > 0`.
  - `tb_ContratoSLA`: `vigente_hasta IS NULL OR vigente_hasta > vigente_desde`.
  - `tb_ContratoSLA_Detalle`: `horas_sla > 0 AND horas_sla <= 720`.
  - `tb_Incidente`: `LEN(LTRIM(RTRIM(titulo))) >= 5`.
  - `tb_Organizacion`: RUT con formato `########-X` (largo entre 9 y 10 y contiene `-`).
  - `tb_Usuario`: `intentos_fallidos >= 0`.
  Antes de aplicar, verificar que los datos existentes (seed y generador) los cumplen.
- **Archivos:** `SentryGuard/Data/SentryGuardDbContext.cs` (`HasCheckConstraint` donde EF lo soporte, o `migrationBuilder.Sql`), `SentryGuard/Migrations/`.
- **Prueba:** en `AuditoriaCategoriaVistasApiTests.cs` (o un archivo nuevo `RestriccionesBdTests.cs`) un test por restricción que intente insertar un valor inválido directo por el `DbContext` y espere `DbUpdateException` con el nombre de la restricción.
- **Aceptación:** `docs/evaluacion3/05_modelo_datos.md` §5.4 sin "Pendientes".

### B2. Auditoría completa: eliminaciones y más entidades (P1)
- **Por qué:** criterio 3 ("auditoría completa de cambios") y RNF-11 ("toda creación, modificación, **eliminación** y exportación"). Hoy `SentryGuardDbContext.SaveChangesAsync` registra altas y modificaciones solo de `Incidente` y `Activo`.
- **Qué hacer:**
  1. Registrar también `EntityState.Deleted` (valores anteriores, sin nuevos) para las entidades auditadas.
  2. Hacer auditables (`IAuditable`) `Organizacion`, `ContratoSLA` y `Usuario` (migración con `fecha_creacion`, `fecha_modificacion`, `creado_por_id`, `modificado_por_id`; las columnas existentes con valor por defecto). Para `Usuario` **excluir `PasswordHash`** de los valores guardados (lista de campos sensibles en `CamposSinAuditar`).
  3. Dejar el punto de extensión para la exportación de reportes (Hito 4): método `RegistrarAccion(entidad, accion)`.
- **Archivos:** `Models/*.cs`, `Data/SentryGuardDbContext.cs`, migración nueva, `Services/IUsuarioActual.cs`.
- **Prueba:** extender `Auditoria_RegistraQuienYCuando...` para eliminar un activo y comprobar el registro con `valoresAnteriores`; test de que el hash de contraseña nunca aparece en `valores_nuevos`.
- **Aceptación:** un `GET /api/v1/auditoria?entidad=Usuario` muestra los cambios sin datos sensibles.

### B3. Decidir y corregir el comportamiento de la IA ante errores del proveedor (P1)
- **Por qué:** criterio 9 y requisito "errores del servicio externo devueltos con el formato de error estándar". Hoy `AsistenteGemini` ante un error HTTP del proveedor usa el simulado sin avisar y solo el tiempo agotado devuelve 504.
- **Qué hacer (propuesta):**
  - Con clave configurada y fallo del proveedor (HTTP no exitoso, JSON inválido, excepción): devolver `ServiceResult.BadGateway()` (502, código `IA_ERROR_PROVEEDOR`) con el detalle sin datos internos. El tiempo agotado sigue en 504.
  - Sin clave: usar el simulado y **decirlo**: agregar a `ClasificacionIaDTO` el campo `Origen` (`"simulado"` o el nombre del modelo) y mostrarlo en la interfaz ("Sugerencia generada por el asistente simulado").
  - Limitar la longitud de lo que se registra del proveedor en los logs.
- **Archivos:** `Services/AsistenteGemini.cs`, `DTOs/DTOs.cs`, `Services/AsistenteSimulado.cs`, `sentryguard-web/src/api/contracts.ts`, `IncidenteForm.tsx`.
- **Pruebas:** actualizar `Gemini_ConErrorDelProveedor_UsaElSimulado...` en `IaApiTests.cs` para esperar 502; agregar pruebas de JSON inválido y de respuesta sin `candidates`.
- **Aceptación:** actualizar ADR-007 (ver E1) con la decisión final.

### B4. Pruebas unitarias de la capa de negocio (P1)
- **Por qué:** criterio 1 (Destacado exige "pruebas unitarias de la capa de negocio"). Hoy solo `IncidenteService` y la IA tienen pruebas unitarias.
- **Qué hacer:** crear `ActivoServiceUnitTests.cs`, `OrganizacionServiceUnitTests.cs` y `AuthServiceUnitTests.cs` (con Moq y EF Core InMemory, como `IncidenteServiceUnitTests.cs`). Casos mínimos:
  - `ActivoService`: Cliente solo ve y crea en su organización (403 si no), eliminar con incidentes da 409, validación de nombre y de la IP o host, orden inválido da 400.
  - `OrganizacionService`: RUT con dígito verificador (válido, inválido, duplicado), contratos SLA superpuestos, finalizar contrato.
  - `AuthService`: hash SHA-256 del refresh token, `ValidaAccesoIncidente` por rol (Analista solo asignados, Cliente solo su organización).
  - `Ordenamiento`: columna válida, descendente, columna inexistente.
- **Aceptación:** al menos 25 pruebas unitarias nuevas; el CI las ejecuta; opcional: reporte de cobertura con `coverlet` (ya referenciado) publicado como artefacto.

### B5. Verificación de seguridad contra el compose (P1)
- **Por qué:** requisito "protección ante al menos cinco vulnerabilidades del OWASP API Security Top 10" con evidencia. `scripts/verificar-seguridad.mjs` apunta a `https://localhost:7166`.
- **Qué hacer:** ejecutar el script contra `API_URL=http://localhost:8088` (ajustar el `ORIGEN_FRONTEND`, el manejo de TLS y los controles que dependen de HTTPS o de CORS). Revisar que los 29 controles sigan siendo válidos tras los cambios (categoría, orden, auditoría, IA) y agregar controles nuevos: `orden` con inyección (400), IA sin rol (403), `X-Forwarded-For` falsificado desde el cliente no evita el límite de login, documentación apagada con `DOCUMENTACION_HABILITADA=false`.
- **Archivos:** `scripts/verificar-seguridad.mjs`, `docs/evidencias/seguridad-api.txt`.
- **Aceptación:** salida completa guardada en `docs/evidencias/seguridad-api.txt`; tabla "OWASP API Top 10 → mitigación → prueba" en `docs/evaluacion3/06_contrato_api.md` (al menos 5 filas con enlace a la prueba).

### B6. CRUD de categorías de incidente (P1)
- **Por qué:** RF-15 dice que el Administrador gestiona las categorías de incidente; hoy solo existe `GET /api/v1/catalogos/categorias-incidente`.
- **Qué hacer:** `POST`, `PUT` y `DELETE` bajo `/api/v1/catalogos/categorias-incidente` (solo Administrador), con las mismas reglas que `categorias-activo` en `CatalogoAPI.cs`: nombre único y entre 3 y 80 caracteres, 409 si está en uso por incidentes, 400 con `errors`. Agregar la pestaña correspondiente en `CatalogosPage.tsx` y en `catalogosService.ts`.
- **Pruebas:** integración (crear, duplicado, en uso, rol) y de componentes.

### B7. Mínimo privilegio en la base (P2)
- **Por qué:** la API se conecta como `sa`. Es una práctica insegura y se menciona en el plan de despliegue (Hito 4).
- **Qué hacer:** crear un login y usuario de aplicación (`sentryguard_app`) con permisos solo sobre la base `SentryGuard` (por ejemplo `db_datareader`, `db_datawriter` y `db_ddladmin` para las migraciones). Opción simple: un script SQL ejecutado por el servicio `seed` o por un servicio `db-init` antes de la API; la contraseña sale de una nueva variable `APP_DB_PASSWORD` en `.env` y `.env.example`, y `scripts/generar-env.*` la genera. La cadena de conexión de `api` y `seed` usa ese usuario; `sa` solo para el health check de la base.
- **Aceptación:** `docker compose up` desde cero sigue funcionando y la API no puede, por ejemplo, `DROP DATABASE`.

### B8. Limpieza del script antiguo y del contrato de la API (P2)
- `SentryGuard_Setup.sql` (raíz) es del Trabajo 1 y ya no refleja las migraciones. Moverlo a `docs/legado/` con una nota o eliminarlo, y quitar cualquier mención en la documentación.
- Regenerar `docs/evaluacion3/contrato-api/openapi-implementado.json` al terminar B1–B6 (con la API en ejecución: `GET /openapi/v1.json`).
- Actualizar `docs/evaluacion3/contrato-api/openapi-evaluacion3.yaml`: hoy describe endpoints planificados (por ejemplo `GET /incidentes/{id}/transiciones`, que **no existe**; el real es `GET /catalogos/transiciones-estado`). Dejarlo igual a lo implementado y validarlo con `npx @redocly/cli lint`.

---

## C. Frontend (interfaz conectada a la API)

> Para cada tarea de esta sección: agregar pruebas de componentes (Vitest + MSW, en `sentryguard-web/src/features/**/*.test.tsx`), correr `npm run lint` y `npm run build`, y verificar con axe y a 360 px. La interfaz debe seguir el sistema de diseño existente (`src/styles/global.css`, componentes de `src/components/`).

### C1. Encabezados ordenables (RF-06) (P1)
- **Por qué:** RF-06 y el requisito "listados con búsqueda, filtros, orden y paginación". La API ya acepta `orden=campo` y `orden=-campo`; la interfaz todavía no ordena.
- **Qué hacer:**
  - Incidentes: columnas ordenables `titulo`, `fecha`, `severidad`, `estado`, `activo`, `categoria`. Activos: `nombre`, `categoria`, `organizacion`. Organizaciones: `razonsocial`, `rut`. Usuarios: `nombre`, `email`, `rol`, `organizacion`.
  - Cada encabezado ordenable es un `<button>` dentro del `<th>` con `aria-sort="ascending|descending|none"`, foco visible, y un icono que no sea el único indicador (texto oculto "ordenado de forma ascendente"). Primer clic ascendente, segundo descendente, tercero quita el orden.
  - El orden va en la URL (`?orden=-fecha`) con `useParametrosListado` para que se conserve al recargar y al compartir el enlace.
  - En móvil (vista de tarjetas de `ListadoResponsivo`) mostrar un `<select>` "Ordenar por" con las mismas opciones.
  - Agregar `orden` a `FiltrosIncidentes` y a las consultas de activos, organizaciones y usuarios.
  - Mostrar el mensaje de la API si responde 400 por orden inválido (no debería ocurrir desde la interfaz).
- **Archivos:** `src/components/ListadoResponsivo.tsx`, `src/hooks/useParametrosListado.ts`, `src/features/incidentes/components/IncidentesTable.tsx` y `IncidentesCards.tsx`, `src/features/incidentes/incidentesService.ts`, y los equivalentes de activos, organizaciones y usuarios.
- **Aceptación:** pruebas que verifican que el clic llama a la API con `orden=-fecha` y que `aria-sort` cambia; axe sin violaciones; funciona solo con teclado.

### C2. Categoría en listados, filtros y detalle (P1)
- **Por qué:** la categoría ya se puede elegir en el formulario, pero no se ve ni se filtra.
- **Qué hacer:** columna y etiqueta "Categoría" en la tabla y las tarjetas de incidentes (texto "Sin categoría" cuando es nula), filtro por categoría en `IncidentesFilters.tsx` (la API acepta `categoriaId`), categoría y "Última modificación" en `IncidenteDetailPage.tsx`, y que `FiltrosIncidentes` y la URL conserven `categoriaId`.
- **Aceptación:** pruebas del filtro y de la visualización; el estado vacío del listado explica los filtros aplicados.

### C3. Pantalla de auditoría con detalle por entidad (RF-16) (P1)
- **Por qué:** `AuditoriaPage.tsx` solo muestra usuario, acción, IP y fecha; la API ya devuelve `entidad`, `entidadId`, `valoresAnteriores` y `valoresNuevos`.
- **Qué hacer:** actualizar `AuditoriaSchema` y `auditoriaService.ts`; mostrar las columnas Entidad e Id; un filtro por entidad (`?entidad=Incidente`); y un detalle expandible (usar `<details>`/`<summary>` nativo) que muestre los cambios como "Campo: antes → después" en lugar del JSON crudo. Sin información sensible.
- **Aceptación:** prueba con una fila de auditoría de modificación; navegable por teclado; legible en 360 px.

### C4. Pruebas de las transiciones de estado en la interfaz (P1)
- **Por qué:** `transiciones.ts` ahora lee la matriz de la API, pero no hay pruebas de `CambiarEstadoForm`.
- **Qué hacer:** tests con MSW que verifiquen: un incidente Abierto ofrece En progreso, Escalado, Resuelto y Cerrado (según el handler); un Resuelto no ofrece Escalado; mientras carga no se muestra "no admite transiciones"; si la API responde 409 `TRANSICION_INVALIDA` se muestra el mensaje del servidor. Ajustar `src/test/handlers.ts` para que la matriz sea la real (14 pares).
- **Archivos:** `src/features/incidentes/incidentes.test.tsx`, `src/test/handlers.ts`.

### C5. Experiencia de la IA en el formulario (P1)
- **Por qué:** criterio 4 ("estados de carga y confirmaciones bien cuidados") y RNF-10.
- **Qué hacer:** mostrar el origen de la sugerencia (ver B3), distinguir el mensaje de 504 ("tardó más de 15 segundos") del de 502 ("el servicio de IA no está disponible"), anunciar el resultado con `aria-live`, y que el botón "Aplicar sugerencia" aplique severidad **y** categoría y confirme con un aviso. Probar los tres estados (éxito, 504, 502) con MSW.
- **Archivos:** `src/features/incidentes/components/IncidenteForm.tsx`, `src/features/ia/iaService.ts`, `src/test/handlers.ts`.

### C6. Verificación de accesibilidad y responsive de lo nuevo (P2, pero obligatoria para el informe)
- **Qué hacer:** correr axe (`e2e/accesibilidad.spec.ts`) sobre las pantallas cambiadas; capturas a 360, 768 y 1440 px de listado, formulario de incidente y auditoría; prueba manual de un flujo completo con teclado y con NVDA (registrar el resultado en `docs/evidencias/revision-manual-accesibilidad.md`, hoy marcado como pendiente en RNF-06); revisar contraste de las etiquetas nuevas y que ningún dato se indique solo con color. Verificar zoom al 200 %.
- **Aceptación:** sin violaciones críticas; capturas en `docs/evidencias/capturas/`; tamaños táctiles de al menos 24 × 24 px (ideal 44).

### C7. Administración de categorías de incidente (P1, depende de B6)
- Ver B6: pestaña "Categorías de incidente" en `CatalogosPage.tsx` con crear, editar y eliminar, confirmación con `ConfirmDialog`, y error 409 legible.

---

## D. Contenedores, CI y seguridad

### D1. Credenciales de desarrollo en el historial de git (P0)
- **Por qué:** la rúbrica dice que las credenciales "no deben subirse al repositorio, **incluido su historial**" y que una credencial real expuesta deja el criterio en Insuficiente. Los commits `7781a81` (y el diff de `ace0f0d`) contienen la contraseña de SA `SentryGuard2026!` y la clave JWT de desarrollo `ClaveSecretaDeDesarrolloSentryGuard…`.
- **Qué hacer:**
  1. Confirmar con `git log --all -S"SentryGuard2026!" --oneline` y `git log --all -S"ClaveSecretaDeDesarrollo" --oneline`.
  2. Como **no se han subido** (`origin/main` aún no tiene esos commits), reescribir esos dos commits antes del primer `push`: lo más simple es un `git rebase -i` sobre `d6461a2` editando `7781a81` (y quitando los valores en `.env.example`, `compose.yaml`), o `git filter-repo --replace-text` con un archivo de reemplazos. Coordinar con Adriel y Bastián: quien tenga clones debe volver a clonar.
  3. Verificar de nuevo con los mismos `git log -S`. Rotar cualquier base o equipo donde esa contraseña se haya usado.
  4. Activar *secret scanning* y *push protection* en GitHub (Settings → Code security).
- **Aceptación:** ambos `git log -S` devuelven vacío.

### D2. Límites de recursos y registros (P2)
- En `compose.yaml`: `mem_limit` para `db` (2 GB mínimo recomendado por Microsoft), límites de CPU y memoria razonables para `api` y `web`, y `logging: driver: json-file, options: max-size: 10m, max-file: 3` para que los registros no llenen el disco.

### D3. Escaneo de imágenes en el CI (P2)
- Agregar un paso con Trivy (`aquasecurity/trivy-action`) que escanee `api` y `web` y falle ante vulnerabilidades `CRITICAL` con corrección disponible. Documentar las excepciones.

### D4. Respaldo programado (P2)
- **Por qué:** RNF-05 declara respaldo diario con retención de 7 diarios y 4 semanales; hoy solo existe el script manual `scripts/respaldo-restauracion.ps1`.
- **Qué hacer:** servicio `backup` en el compose (imagen `mcr.microsoft.com/mssql-tools` o la propia de SQL Server) que ejecute cada 24 h `BACKUP DATABASE … WITH COMPRESSION, CHECKSUM` en un volumen separado `backups`, aplique la retención y deje un registro. Probar una restauración desde ese archivo. Documentarlo en `docs/evaluacion3/05_modelo_datos.md` §5.6.

### D5. HTTPS para la demostración (P2)
- Perfil opcional de compose (`--profile tls`) con nginx sirviendo HTTPS con un certificado autofirmado generado por un script, para mostrar `Strict-Transport-Security`; en el plan de despliegue se describe el certificado real (Hito 4). No es obligatorio para el Hito 3.

---

## E. Documentación coherente con el código (criterios 1 y 10)

> La rúbrica penaliza las discrepancias entre documentación y código. Estas son las que existen hoy.

### E1. ADR-007 dice Anthropic y el código usa Gemini (P1)
- `docs/evaluacion3/04_decisiones_adr.md` (ADR-007) describe `AsistenteAnthropic`, `AsistenteFalso` y el modelo `claude-haiku-5-5`; el código tiene `AsistenteGemini` (API de Google) y `AsistenteSimulado`, con modelo `gemini-2.5-flash` y la variable `IA__ApiKey`. **Decidir:** (a) cambiar el adaptador a Anthropic (el patrón Adapter lo permite: una clase nueva `AsistenteAnthropic` y la clave `IA_API_KEY`), o (b) corregir el ADR y todos los documentos (`01`, `03`, `06`, `README`) para decir Gemini, con las alternativas descartadas. Incluir la decisión de B3.

### E2. ADR-010 dice "interceptor de EF Core" (P1)
- Lo implementado es una sobrescritura de `SaveChangesAsync` en `SentryGuardDbContext` con `IUsuarioActual`. Actualizar ADR-010 (decisión, alternativas: interceptor, triggers, tablas temporales) y mencionar que el detalle va en `tb_LogAuditoria`.

### E3. Nuevas decisiones que faltan registrar (P1)
Agregar ADR-012 en adelante, con alternativas descartadas:
- .NET 10 en lugar de .NET 9.
- SPA y API en el mismo origen con nginx como proxy (`VITE_API_URL=same-origin`) frente a CORS entre orígenes.
- Imágenes *chiseled-extra*, nginx sin privilegios y health check propio (`--healthcheck`) frente a instalar `curl`.
- Migraciones al iniciar la API (`Database__MigrarAlIniciar`) frente a un contenedor de migración; y su riesgo en producción (ver Hito 4).
- `orden` con lista blanca de columnas.
- La categoría de incidente como tabla y no como enumeración.

### E4. Diagramas y diccionario de datos (P1)
- `diagramas/componentes.mmd` y `despliegue.mmd` deben reflejar el compose real: servicios `web`, `api`, `db`, `seed`; redes `app` y `datos`; puerto 8088; proxy `/api`; volumen `sqldata`; variables. Volver a exportar `.png` y `.svg` (`npx @mermaid-js/mermaid-cli -i x.mmd -o x.svg`).
- `er-actual.mmd` y el diccionario de `05_modelo_datos.md` §5.1 y §5.3 se generaron desde el *snapshot* de EF con un script que **no está en el repositorio**. Recrearlo en `scripts/generar-er.mjs` (lee `SentryGuardDbContextModelSnapshot.cs` y escribe el diagrama y las tablas), regenerar con las tablas nuevas (`tb_CategoriaIncidente`, `tb_TransicionEstado`), las columnas de auditoría y las 3 vistas, y unificar `er-actual` con `er-cambios-hito2` (ya no hay "cambios planificados").
- `flujo-estados-incidente.mmd` debe coincidir con los 14 pares de `tb_TransicionEstado`.

### E5. Estado de los requerimientos (P1)
- En `02_requerimientos_historias.md` cambiar a ✅ (con la evidencia real) los que ya se cumplen: RF-06 (backend; 🟡 hasta C1), RF-09, RF-16 (🟡 hasta B2 y C3), RF-23, RF-24, RF-25, RNF-04, RNF-05, RNF-08, RNF-09, RNF-10, RNF-11 (🟡 hasta B2), RNF-12 (✅ cuando A1 pase). Agregar a cada fila el archivo y la prueba que lo verifica.

### E6. README y registro de IA (P1)
- `README.md`: el cuadro de pruebas dice 23 de integración y 32 de componentes (ahora 73 de backend, incluidas unitarias, y 35); actualizar también el árbol de carpetas, la sección de IA (declarar **cada herramienta y propósito**: Claude Code, Antigravity y las demás que use cada integrante), y mantener "Ejecución con Docker" como camino principal.
- `AI_LOG.md`: cada integrante debe dejar su entrada con prompts, lo aceptado, lo corregido y lo que entendió (sección 5 "Notas del estudiante" está vacía). Es lo que cada uno explicará en la defensa.

### E7. Hito 1 pendiente (P2)
- Registrar la aprobación del dominio por el docente en `01_propuesta_dominio.md`, confirmar el integrante 3 y las fechas en `08_plan_trabajo.md` §8.1 y §8.2 (pendientes listados en `docs/evaluacion3/README.md`).

### E8. Trazabilidad por asignatura (P2)
- Empezar la tabla requerimiento → componente → asignatura (OO II, Base de Datos, BI I y II, Web II) que pide el criterio 10: por ejemplo, `RF-16` → `SentryGuardDbContext.SaveChangesAsync` + `tb_LogAuditoria` → Base de Datos y OO II. Servirá para el informe final.

## 9. Reparto sugerido

| Persona | Tareas |
|---|---|
| **Rafael** (desarrolla con IA) | A1, A2, D1, B1, B2, B3, B4, B6, D2, D3 |
| **Adriel** (frontend y accesibilidad) | A3, A4, C1–C7, E6 (parte de la interfaz) |
| **Bastián** (base de datos y documentación) | B5, B7, B8, D4, E1–E5, E7, E8 |

Cada persona hace commits propios de su área (el historial de git es evidencia de aporte individual: hoy Adriel 33, Bastián 19 y Rafael 12).

## 10. Plan de trabajo por orden

1. **Hoy:** D1 (limpiar el historial), A1 (subir y ver el CI), A2 y A3 (probar el compose en una máquina limpia y en un navegador).
2. **Paralelo:** B1, B2, B3, B4, B6 (Rafael); C1–C5 y C7 (Adriel); E1–E6 (Bastián).
3. **Cierre:** B5, A4, A5, C6, B8 y la revisión final de la lista de verificación.

## 11. Lista de verificación final del Hito 3

- [ ] `git clone` + `generar-env` + `docker compose up --build` funciona en una máquina limpia (A2).
- [ ] El CI está en verde en GitHub y publica las imágenes solo desde `main` (A1).
- [ ] Ninguna credencial en el historial (D1).
- [ ] Las 4 pruebas de contenedores: `/health`, SPA, API por el proxy y `seed` con código 0.
- [ ] Interfaz conectada a la API, con orden, filtros, categoría y auditoría por entidad (C1–C3).
- [ ] Endpoint de IA documentado, con errores 502 y 504 en el formato estándar y pruebas con el servicio simulado (B3).
- [ ] 100 % de las restricciones CHECK del modelo (B1) y de la auditoría (B2).
- [ ] Pruebas: backend ≥ 100 (con unitarias de la capa de negocio), frontend con pruebas de lo nuevo, E2E contra el compose.
- [ ] La documentación coincide con el código: ADR, diagramas, diccionario, requerimientos, README (E1–E6).
- [ ] Cada integrante tiene commits propios y su entrada en `AI_LOG.md`.
