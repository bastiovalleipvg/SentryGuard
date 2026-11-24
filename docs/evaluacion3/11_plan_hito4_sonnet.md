# 11. Plan para terminar el proyecto con Claude Sonnet (Hito 4 y entrega)

**Fecha:** 22 de noviembre de 2026 · **Responsables:** Rafael Navarrete y Adriel Troncoso.

**Estado de partida:** `main` no compila desde el 13 de noviembre (código duplicado al juntar el trabajo de Bastián y Rafael), el CI está en rojo, faltan el dashboard, los reportes y el plan de despliegue, y la documentación tiene diferencias con el código. Todo lo demás de los Hitos 2 y 3 está hecho (ver `10_pendientes_hito3.md`).

## 0. Cómo usar este plan con Sonnet

1. **Elegir el modelo:** en Claude Code, `/model` y elegir **Sonnet**. En la app de escritorio, en el selector de modelo.
2. **Una tarea por sesión.** Al terminar una tarea, `/clear` (o una sesión nueva) antes de la siguiente. Sonnet rinde mejor con un objetivo acotado que con «haz el hito 4».
3. **Empezar en modo plan** (Shift + Tab hasta «plan mode»), pegar el **bloque de contexto** de la sección 1 y después el **prompt de la tarea**. Leer el plan que propone, corregirlo si algo no cuadra y recién entonces aprobarlo.
4. **No aceptar «listo» sin pruebas.** Cada tarea trae su sección *Verificación*: hay que correr esos comandos (o pedirle a Sonnet que los corra) y leer el resultado.
5. **Los commits los hace la persona, no Sonnet.** El prompt le pide terminar con la lista de archivos y un mensaje sugerido. Revisa el `git diff` y haz el commit tú mismo, para que quede con tu nombre y sin la línea `Co-Authored-By`.
6. **Si Sonnet se atasca** (repite el mismo error dos veces o empieza a tocar archivos que no corresponden): detenerlo, `/clear`, y volver a pegar el prompt agregando lo que se aprendió («el error era X; no toques Y»).

## 1. Bloque de contexto (pegar al inicio de cada sesión)

> Proyecto: SentryGuard, gestión de incidentes de ciberseguridad para una empresa ficticia (CyberOps Chile SpA), con datos sintéticos. Evaluación 3 de Lenguaje de Programación Web II. Monorepo:
> - `SentryGuard/`: API ASP.NET Core .NET 10 con Minimal APIs, EF Core 10 y SQL Server 2022. Arquitectura: endpoints delgados (`Endpoints/`) → servicios con interfaz (`Services/`, devuelven `ServiceResult<T>` de `Common/ServiceResult.cs`) → repositorios (`Repositories/`) → `Data/SentryGuardDbContext.cs`. Errores con ProblemDetails (RFC 9457) mediante `Common/ApiResults.cs`, con extensión `codigo`. Paginación con `Endpoints/Paginacion.cs` (máx. 50); orden con `Common/Ordenamiento.cs`.
> - `SentryGuard.Tests/`: xUnit; unitarias de servicios e integración con `WebApplicationFactory` contra la base `SentryGuardDB_Test`.
> - `sentryguard-web/`: React 19 + TypeScript + Vite, React Router 8, TanStack Query, React Hook Form + Zod 4, Vitest + MSW 3 (la opción es `onUnhandledFrame`, no `onUnhandledRequest`), Playwright + axe. Organización por *features* (`src/features/*`). Cliente HTTP único `src/api/httpClient.ts` (`apiRequest`), contratos Zod en `src/api/contracts.ts`, permisos en `src/features/auth/permisos.ts`, estilos y tokens en `src/styles/global.css`, componentes reutilizables en `src/components/` (`ListadoResponsivo`, `FiltrosListado`, `ConsultaRecurso`, `ResumenErrores`, `FormField`, `ConfirmDialog`, `Icono`, `Pagination`).
> - `compose.yaml` (servicios `db`, `api`, `seed`, `web`), `.github/workflows/ci.yml`, `scripts/` (`generar-env.ps1|sh`, `respaldo-restauracion.ps1`, `verificar-seguridad.mjs`), documentación en `docs/evaluacion3/`.
> - Roles: Administrador, Supervisor, Analista, Cliente. El Cliente solo ve su organización; el Analista solo sus incidentes asignados (`AuthService.ValidaAcceso*`).
> - La IA usa Gemini detrás de `Services/IAsistenteIA.cs` (`AsistenteGemini`, y `AsistenteSimulado` para pruebas y CI).
>
> Reglas obligatorias:
> 1. Todo el texto visible y los mensajes de error, en español.
> 2. No hagas `git add`, `git commit` ni `git push`: al terminar, dame la lista de archivos cambiados y un mensaje de commit con el formato `tipo(alcance): descripcion` en español, minúsculas y sin tildes.
> 3. Antes de decir que algo funciona, corre `dotnet build SentryGuard.slnx` y `dotnet test SentryGuard.slnx` (backend) o `npm run lint`, `npx tsc -b` y `npm test` en `sentryguard-web/` (frontend), y muéstrame el resultado.
> 4. Antes de cambiar `Data/SentryGuardDbContext.cs`, cualquier archivo de `Models/` o una migración, muéstrame el diff y espera mi aprobación.
> 5. No toques archivos que no sean de esta tarea. No subas secretos ni archivos `.env`.
> 6. Cada endpoint nuevo exige el rol con `RequireAuthorization(p => p.RequireRole(...))` y valida el acceso por organización o asignación en el servicio.
> 7. Accesibilidad WCAG 2.2 AA en la interfaz: etiquetas asociadas, foco visible, sin depender solo del color, contraste de al menos 4,5:1.
> 8. Si cambias el contrato, el modelo o la arquitectura, actualiza el documento correspondiente de `docs/evaluacion3/`.

## 2. Orden y dependencias

| Cuándo (estimado) | Rafael | Adriel | Punto de encuentro |
|---|---|---|---|
| 22 y 23 nov. | **R1** compilación y CHECK · R2 Dependabot | **A0** `.gitignore` · A4 (parte de las ADR) | R1 en `main` con el CI en verde: desde ahí todos parten de una base que compila |
| 24 a 26 nov. | **R3** endpoints del dashboard · **R4** endpoints de reportes | **A1** pantalla del dashboard · **A2** pantalla de reportes (con datos simulados en MSW hasta que R3 y R4 estén en `main`) | El 26: dashboard y reportes conectados a la API real (Hito 4) |
| 27 a 30 nov. | **R5** plan de despliegue · **R6** pruebas en máquina limpia | **A3** accesibilidad · **A4** documentación | Indicadores calculados con R3, para el informe |
| 1 y 2 dic. | Revisión del informe y de la presentación | **A5** informe, manual, ejemplos exportados, presentación | Ensayo cronometrado los dos juntos |
| 3 dic. | Entrega | Entrega | — |

---

## 3. Tareas de Rafael (backend, base de datos y DevOps)

### R1. Arreglar la compilación y las restricciones CHECK (urgente)

**Por qué:** sin compilación no hay CI, pruebas ni demostración. Criterios 2 y 3 de la rúbrica.

**Prompt para Sonnet:**
> `main` no compila. `dotnet build SentryGuard.slnx` da 10 errores CS0102 (definición duplicada) porque dos personas implementaron lo mismo en paralelo y se juntaron las dos versiones. Los duplicados están en:
> - `SentryGuard/Models/Incidente.cs`: `CategoriaId` (líneas 33 y 48) y `Categoria` (líneas 75 y 84).
> - `SentryGuard/Models/LogAuditoria.cs`: `Entidad`, `EntidadId`, `ValoresAnteriores` y `ValoresNuevos`.
> - `SentryGuard/DTOs/DTOs.cs`: `CategoriaId` y `Categoria` en `IncidenteDTO`.
> - `SentryGuard/Data/SentryGuardDbContext.cs`: `CategoriasIncidente` (líneas 27 y 40) y **dos implementaciones de la auditoría**, cada una con su propio `CamposSinAuditar` (cerca de las líneas 49 y 545).
>
> Pasos:
> 1. Para cada duplicado, compara las dos versiones con la migración `Migrations/20261113131922_AuditoriaCategoriaTransicionesVistas.cs` y con `Migrations/SentryGuardDbContextModelSnapshot.cs`. Conserva la que coincide con el esquema (nombres de columna, tipos, largos).
> 2. En la auditoría, deja **una sola** implementación que registre crear, modificar **y eliminar** (`EntityState.Deleted` → operación `ELIMINAR`), con los valores anteriores y nuevos, y que ignore `RowVersion` y los campos de control. Explícame qué tomaste de cada versión.
> 3. Muéstrame el diff de `SentryGuardDbContext.cs` y de `Models/` antes de aplicarlo.
> 4. Cuando compile, crea la migración `AgregaRestriccionesCheck` con `dotnet ef migrations add AgregaRestriccionesCheck --project SentryGuard`. **Revísala:** solo debe agregar las restricciones CHECK que el `DbContext` declara y la base no tiene (hoy solo existen `CK_tb_TransicionEstado_OrigenDistintoDestino` y `CK_tb_Incidente_FechaCierre`). Si la migración trae cualquier otro cambio, detente y avísame.
> 5. Antes de aplicar la migración, verifica que los datos sintéticos cumplen las nuevas CHECK (por ejemplo, ningún título de menos de 5 caracteres). Si alguno no cumple, corrige el generador, no la restricción.
> 6. Corre todas las pruebas.

**Verificación:** `dotnet build SentryGuard.slnx` sin errores ni advertencias nuevas · `dotnet test SentryGuard.slnx` en verde · `docker compose up --build` arranca y el `seed` termina con código 0 · el CI de GitHub en verde después del push.

**Commits sugeridos:** `fix(backend): unificar definiciones duplicadas al integrar categoria y auditoria` y `feat(db): migracion con las restricciones check del modelo`.

### R2. Revisar los PR de Dependabot

**Prompt para Sonnet:**
> Lista los PR abiertos de Dependabot con `gh pr list --author app/dependabot`. Clasifícalos: (a) los que suben de versión mayor de .NET, EF Core o el SDK (por ejemplo, SDK 11 o EF Tools 11: el proyecto usa .NET 10 y deben cerrarse con un comentario que lo explique); (b) acciones de GitHub, imágenes de Docker y paquetes de npm o NuGet de versión menor o de parche. Para los de tipo (b), dime cuáles tienen el CI en verde después de que `main` compila. No los fusiones tú: dame la lista y los comandos `gh pr merge` y `gh pr close` para que yo los ejecute.

**Verificación:** ningún PR de versión mayor fusionado; el CI de `main` sigue en verde después de cada fusión.

### R3. Endpoints del dashboard

**Por qué:** criterios 5 y 6 (indicadores y dashboard alimentado por el backend). El contrato ya está definido en `docs/evaluacion3/contrato-api/openapi-evaluacion3.yaml` (`/api/v1/dashboard/indicadores` y `/api/v1/dashboard/series`).

**Prompt para Sonnet:**
> Implementa el dashboard del backend siguiendo **exactamente** el contrato de `docs/evaluacion3/contrato-api/openapi-evaluacion3.yaml` (esquemas `Indicadores`, `Indicador`, `SeriesDashboard`, `PuntoSerie`).
> 1. Crea `Services/IDashboardService.cs` y `Services/DashboardService.cs`, que devuelven `ServiceResult<T>`, y `Endpoints/DashboardAPI.cs` con `GET /api/v1/dashboard/indicadores` y `GET /api/v1/dashboard/series`. Regístralos en `Program.cs`.
> 2. Filtros por query: `desde`, `hasta` (por defecto, los últimos 12 meses; `hasta` debe ser mayor o igual que `desde`, si no, 400 con error por campo), `organizacionId`, `severidadId` y `categoriaId`.
> 3. Visibilidad: Administrador y Supervisor ven todo; el Cliente solo su organización (ignora el `organizacionId` que mande); el Analista solo sus incidentes asignados.
> 4. Calcula en SQL, usando las vistas `vw_IncidenteDetalle`, `vw_CumplimientoSla` y `vw_IndicadoresMensuales` (modelos en `Models/Vistas.cs`), nunca cargando todas las filas en memoria. Estos 6 indicadores, cada uno con `clave`, `nombre`, `valor`, `unidad`, `formula` y `variacion` respecto del período anterior de la misma duración:
>    - `volumen`: cantidad de incidentes creados en el período.
>    - `cumplimientoSla`: cerrados dentro del plazo / cerrados × 100.
>    - `tiempoMedioResolucion`: promedio de horas entre la creación y el cierre, de los cerrados.
>    - `tiempoMedioAsignacion`: promedio de horas entre la creación y la entrada «Analista asignado» de `tb_BitacoraIncidente`.
>    - `backlog`: incidentes no Resueltos ni Cerrados al final del período.
>    - `proporcionCriticos`: críticos / total × 100.
> 5. Series: `incidentesPorMes`, `cumplimientoSlaPorMes`, `porSeveridad`, `porCategoria`, `porOrganizacion` (vacía para el Cliente) y `cargaPorAnalista` (incidentes abiertos por analista; vacía para el Cliente y el Analista).
> 6. Pruebas de integración en `SentryGuard.Tests/DashboardApiTests.cs`: sin token 401; el Cliente solo ve su organización; el Analista solo lo suyo; `hasta < desde` da 400; con datos creados en la prueba, los valores de `volumen` y `cumplimientoSla` son los esperados. Pruebas unitarias de las fórmulas en `DashboardServiceUnitTests.cs`.
> 7. Actualiza `docs/evaluacion3/06_contrato_api.md` (pasar el dashboard a implementado) y vuelve a exportar `contrato-api/openapi-implementado.json` desde `/openapi/v1.json`.

**Verificación:** las pruebas nuevas en verde · con el `seed` cargado, `GET /api/v1/dashboard/indicadores` como Administrador responde en menos de 1,5 s (RNF-01) · los números coinciden con una consulta SQL hecha a mano sobre la vista.

**Commits sugeridos:** `feat(api): endpoints de indicadores y series del dashboard` y `test(api): pruebas del dashboard por rol y filtros`.

### R4. Endpoints de los 3 reportes con exportación

**Por qué:** criterio 7 (reportes con Excel, PDF y CSV, filtros, permisos y registro de quién exportó).

**Prompt para Sonnet:**
> Implementa los reportes siguiendo el contrato de `docs/evaluacion3/contrato-api/openapi-evaluacion3.yaml` (`/api/v1/reportes/{tipo}` y `/api/v1/reportes/{tipo}/exportar`, esquema `PaginaReporte`), con `tipo` = `operativo` (detalle de incidentes), `gerencial` (totales por organización, severidad y estado) o `sla` (cumplimiento y tiempo de resolución por mes dentro de un rango obligatorio de fechas).
> 1. `Services/IReporteService.cs` y `ReporteService.cs`, que calculan las filas y los totales desde las vistas, con los mismos filtros que el dashboard más `estadoId`, orden y paginación.
> 2. Patrón **Strategy** para la exportación: interfaz `IExportador` (un método que recibe el reporte con sus filtros y escribe en un `Stream`) con tres implementaciones:
>    - `ExportadorExcel` con ClosedXML: encabezados en negrita, fechas en formato `dd-MM-yyyy HH:mm`, números con formato, fila de totales y columnas ajustadas.
>    - `ExportadorPdf` con QuestPDF: título, fecha de emisión, filtros usados, numeración «Página X de Y» y texto seleccionable. **Antes de agregar QuestPDF, revisa su licencia actual** y configura `QuestPDF.Settings.License` según corresponda; anota la decisión en la ADR-005.
>    - `ExportadorCsv`: UTF-8 con BOM, separador punto y coma (`;`), comillas cuando corresponda.
> 3. Permisos: Administrador y Supervisor, todos los reportes; Cliente, solo su organización; Analista, 403.
> 4. Cada exportación queda en `tb_LogAuditoria` con la operación `EXPORTAR`, el usuario, el tipo de reporte, el formato y los filtros.
> 5. Límite de tasa de 20 exportaciones por minuto por usuario (política `exportaciones`, deshabilitada en el entorno `Testing`, igual que las otras).
> 6. Respuesta con `Content-Disposition: attachment; filename="reporte-{tipo}-{fecha}.{ext}"` y el `Content-Type` correcto; genera el archivo en *streaming*.
> 7. Pruebas en `ReportesApiTests.cs`: permisos por rol; el Excel se puede abrir con ClosedXML y trae los encabezados y la fila de totales; el PDF empieza con `%PDF`; el CSV empieza con el BOM y usa `;`; los filtros se respetan; queda la fila de auditoría.
> 8. Actualiza la ADR-005, `06_contrato_api.md` y `openapi-implementado.json`.

**Verificación:** las pruebas en verde · descarga manual de los 3 formatos de cada reporte y apertura en Excel y en un lector de PDF · guardar un ejemplo de cada formato en `docs/evaluacion3/ejemplos/` (los pide la pauta).

**Commits sugeridos:** `feat(api): reportes operativo, gerencial y de sla`, `feat(api): exportacion a excel, pdf y csv con patron strategy` y `test(api): pruebas de reportes y exportacion`.

### R5. Plan de despliegue y prueba de reversión

**Por qué:** criterio 8 exige el plan con los 10 apartados; el nivel «Destacado» pide la reversión probada y los costos fundamentados.

**Prompt para Sonnet:**
> Escribe `docs/evaluacion3/12_plan_despliegue.md` con los 10 apartados que exige la pauta, en este orden y basado en lo que **realmente** existe en `compose.yaml`, `.github/workflows/ci.yml`, `scripts/` y `.env.example`:
> 1. Entornos (desarrollo, pruebas y producción) y sus diferencias de configuración.
> 2. Infraestructura propuesta (una VM o un servicio de contenedores), recursos estimados (SQL Server necesita unos 2 GB de RAM), puertos, red, dominio y certificado HTTPS.
> 3. Pasos de despliegue, en orden y con sus comandos.
> 4. Secretos y variables por entorno (`.env`, secretos de GitHub), sin ningún valor real.
> 5. Actualización y reversión, incluidas las migraciones (`dotnet ef database update <migracion_anterior>`) y el regreso a la etiqueta de imagen anterior.
> 6. Respaldo y recuperación: frecuencia, RTO de 30 min y RPO de 24 h (usa `scripts/respaldo-restauracion.ps1`).
> 7. Monitoreo y logs: qué se mide (`/health`, errores 5xx, latencia), qué alertas existen y dónde se ven.
> 8. Seguridad en producción: cabeceras, CORS, endurecimiento de contenedores y actualización de imágenes (Dependabot y Trivy).
> 9. Costos y riesgos: costo mensual estimado con fuente y fecha de consulta del precio, y riesgos con su mitigación.
> 10. Lista de verificación previa a salir a producción.
>
> Después, **prueba una reversión** y deja la evidencia en `docs/evaluacion3/evidencias/reversion.md` (comandos y salida): levantar la versión actual, desplegar una versión con un cambio visible, volver a la imagen anterior y revertir una migración de prueba, comprobando que el sistema sigue sano.

**Verificación:** los 10 apartados presentes · cada comando del documento se ejecutó al menos una vez · la evidencia de la reversión existe.

### R6. Pruebas en máquina limpia y limpieza final

**Prompt para Sonnet:**
> 1. Haz un clon limpio del repositorio en una carpeta nueva y sigue **solo** el README: `scripts/generar-env`, `docker compose up --build`. Anota cualquier paso que falte o falle, y corrige el README.
> 2. Con el sistema en contenedores, corre `npm run test:e2e` (ajusta `baseURL` por variable de entorno si hace falta) y `node --use-system-ca scripts/verificar-seguridad.mjs`, y guarda las salidas en `docs/evidencias/`.
> 3. Crea un archivo `.mailmap` en la raíz que una las identidades `BonifacioAnacleto` y `Rafael-Ano` (el mismo correo) bajo «Rafael Navarrete», y verifica con `git shortlog -sne`.

---

## 4. Tareas de Adriel (frontend, accesibilidad y documentación)

### A0. Corregir `.gitignore` (urgente, 10 minutos)

**Por qué:** una sustitución automática dejó la línea `!.env`, que vuelve a incluir los archivos `.env` (con claves de IA, JWT y SQL Server).

**Prompt para Sonnet:**
> En `.gitignore`, la sección «Variables de entorno locales» quedó corrupta: tiene líneas como `*.zip.example)`, `*.zip.*` y `!.env`. Reemplázala por:
> ```
> # Variables de entorno locales (solo se versiona la plantilla .env.example)
> .env
> .env.*
> !.env.example
> ```
> Comprueba con `git check-ignore -v .env sentryguard-web/.env SentryGuard/.env` que los tres quedan ignorados, y con `git ls-files | grep -i env` que solo están versionados los `.env.example`. Revisa que `docs/docs.zip` siga ignorado.

**Commit sugerido:** `fix: restaurar la exclusion de archivos .env en gitignore`.

### A1. Pantalla del dashboard

**Por qué:** criterio 6 (4 o más visualizaciones, filtros interactivos, datos del backend, gráficos accesibles) y RF-24 (resumen con IA en la interfaz).

**Prompt para Sonnet:**
> Crea la feature `sentryguard-web/src/features/dashboard/` con la ruta `/dashboard`, siguiendo la maqueta `docs/evaluacion3/maquetas/propuesta-dashboard-escritorio.png` (HTML de referencia en `maquetas/fuente/propuesta-dashboard.html`) y el contrato de `/api/v1/dashboard/indicadores` y `/series` de `docs/evaluacion3/contrato-api/openapi-evaluacion3.yaml`.
> 1. `dashboardService.ts` con esquemas Zod en `src/api/contracts.ts`, y hooks de TanStack Query.
> 2. Filtros en la URL con `useParametrosListado` (`desde`, `hasta`, `organizacionId` solo para el staff, `severidadId` y `categoriaId`) usando `FiltrosListado`.
> 3. Seis tarjetas de indicadores con su valor, la fórmula en texto pequeño y la variación (con texto y signo, no solo color).
> 4. Cuatro o más gráficos como **componentes SVG propios** en `components/` (`GraficoBarras`, `GraficoLinea`, `GraficoBarrasHorizontales`), sin agregar librerías. Cada gráfico lleva un `h2`, una descripción textual de la conclusión enlazada con `aria-labelledby`, `role="img"` en el SVG y un `<details>` con una tabla de los datos. Puedes basarte en el gráfico de `frontend/src/pages/PanelPage.tsx` del repositorio de Bastián (`SentryGuard-2`), adaptándolo a los tokens de `global.css`.
> 5. Panel «Resumen» con un botón «Resumir con IA» que usa `useResumirIa` de `src/features/ia/iaService.ts` con los mismos filtros; muestra la etiqueta «Generado con IA», se anuncia con `aria-live="polite"` y, si la IA falla (502 o 504), muestra el mensaje del error sin romper la pantalla.
> 6. Menú y permisos: agrega `verDashboard` a `permisos.ts` (los 4 roles; la API filtra lo que ve cada uno) y el enlace en `components/Layout.tsx` con el ícono que corresponda.
> 7. Mientras el backend no esté en `main`, prueba con MSW; agrega los handlers en `src/test/handlers.ts`.
> 8. Pruebas en `src/features/dashboard/dashboard.test.tsx`: se ven los 6 indicadores; cambiar un filtro cambia la URL y vuelve a pedir los datos; cada gráfico tiene su tabla de datos; el resumen con IA se anuncia; un 502 muestra el error.

**Verificación:** `npm run lint`, `npx tsc -b` y `npm test` en verde · sin desplazamiento horizontal a 360 px · se puede usar solo con el teclado.

**Commits sugeridos:** `feat(ui): pantalla de dashboard con indicadores y graficos accesibles` y `feat(ui): resumen del dashboard con ia`.

### A2. Pantalla de reportes con exportación e impresión

**Por qué:** criterio 7 (exportar con los filtros aplicados y una versión imprimible limpia).

**Prompt para Sonnet:**
> Crea la feature `sentryguard-web/src/features/reportes/` con la ruta `/reportes`, siguiendo `docs/evaluacion3/maquetas/propuesta-reportes-escritorio.png` y el contrato de `/api/v1/reportes/{tipo}` y `/exportar`.
> 1. Selector del tipo de reporte (operativo, gerencial, SLA) como navegación con `aria-current`, filtros en la URL, tabla con `ListadoResponsivo`, totales, orden y paginación.
> 2. Botones «Exportar Excel», «Exportar PDF» y «Exportar CSV»: agrega a `src/api/httpClient.ts` una función `descargarArchivo(ruta, query)` que adjunta el Bearer, recibe un `Blob`, toma el nombre del archivo de `Content-Disposition` y dispara la descarga. Mientras descarga, el botón muestra «Generando…» y queda deshabilitado; los errores (403, 429) se muestran con `mensajeDeError`.
> 3. Botón «Imprimir» (`window.print()`) y estilos `@media print` en `global.css`: ocultar la barra lateral, el menú, los filtros y los botones; mostrar el título del reporte, la fecha de emisión y los filtros usados; tabla legible en blanco y negro.
> 4. Permisos: agrega `verReportes` a `permisos.ts` (Administrador, Supervisor y Cliente; el Analista no) y el enlace en el menú.
> 5. Pruebas en `reportes.test.tsx`: cambiar el reporte y los filtros; la descarga llama a la ruta correcta con el formato elegido; el Analista ve «Acceso denegado» en `/reportes`.

**Verificación:** las pruebas en verde · imprimir a PDF desde el navegador y revisar que no salgan menús ni botones · guardar capturas de la vista de impresión en `docs/evaluacion3/evidencias/` (las pide la pauta).

### A3. Accesibilidad y responsive de todo el sistema

**Por qué:** criterio 4 (el de mayor puntaje, 15 pts): axe sin errores, prueba con lector de pantalla documentada y responsive de 360 a 1440 px.

**Prompt para Sonnet:**
> En `sentryguard-web/e2e/accesibilidad.spec.ts`:
> 1. Agrega a la auditoría con axe las vistas nuevas (dashboard, reportes, el formulario de incidente con la sugerencia de IA, la auditoría con detalle y los catálogos).
> 2. Cambia los anchos de las capturas a 360, 390, 768, 1024 y 1440 px, comprobando que no haya desplazamiento horizontal en ninguno.
> 3. Agrega una prueba con `page.emulateMedia({ reducedMotion: 'reduce' })` y otra con zoom al 200 % (un viewport de 640 px con el texto al doble) en el dashboard.
> Corre la suite contra el sistema levantado y regenera `docs/evidencias/axe-reporte.json` y las capturas.

**Tarea manual (no la puede hacer Sonnet):** recorrer un flujo completo con **NVDA** y solo con el teclado (login → crear incidente con la sugerencia de IA → ver el dashboard → exportar un reporte) y anotar lo que se anuncia en cada paso en `docs/evidencias/revision-manual-accesibilidad.md`. Sonnet puede preparar la tabla para llenarla. Opcional: un informe de **Lighthouse** de las vistas principales.

### A4. Coherencia entre la documentación y el código

**Por qué:** criterios 1 y 10: la pauta penaliza las diferencias entre la documentación y el código.

**Prompt para Sonnet:**
> Alinea `docs/evaluacion3/` con el código actual:
> 1. **ADR-007** (`04_decisiones_adr.md`): la IA se implementó con Gemini (`Services/AsistenteGemini.cs`), no con Anthropic. Reescríbela con la decisión real, el motivo y la alternativa descartada.
> 2. **ADR-010**: describe el mecanismo de auditoría que quedó en `SentryGuardDbContext.cs` después de R1 (sobrescritura de `SaveChanges` o interceptor; revisa cuál es).
> 3. Revisa el resto de las ADR y la sección 3.1 de `03_arquitectura.md` («estado actual y objetivo»): marca como hecho lo que ya existe (servicios, `ServiceResult`, ProblemDetails, adaptador de IA, tabla de transiciones, datos sintéticos).
> 4. Crea `scripts/generar-er.mjs` que lea `SentryGuard/Migrations/SentryGuardDbContextModelSnapshot.cs` y genere el diagrama ER en Mermaid y las tablas del diccionario de datos (columna, tipo, nulo, clave, restricción). Úsalo para regenerar las secciones 5.1 y 5.3 de `05_modelo_datos.md` y `diagramas/er-actual.mmd`, con las tablas nuevas, las columnas de auditoría, las restricciones CHECK y las 3 vistas. Une `er-actual` con `er-cambios-hito2` (ya no son cambios planificados).
> 5. `01_propuesta_dominio.md` y `diagramas/flujo-estados-incidente.mmd`: el diagrama debe tener exactamente los pares de `tb_TransicionEstado` sembrados en la base.
> 6. `02_requerimientos_historias.md`: actualiza la columna «Estado» de cada RF y RNF según el código.
> 7. Vuelve a exportar los diagramas a SVG y PNG.

### A5. Entregables finales

**Por qué:** criterios 5, 10 y 11, y la lista de entregables de la pauta.

Conviene hacerlo en sesiones separadas, una por documento:

1. **Informe técnico** (`docs/evaluacion3/INFORME_TECNICO_EV3.md`, exportado a PDF), con las 11 secciones de la pauta. El prompt para Sonnet debe pedirle que **reutilice** los documentos 01 a 12 (resumir y enlazar, no inventar) y que la sección 8 use los valores reales que entrega `/api/v1/dashboard/indicadores` con el seed cargado. Para cada indicador: fórmula, pregunta de negocio, valor, tendencia e interpretación, y al final **una recomendación de negocio** (lo pide el nivel «Destacado»).
2. **Informe de integración del módulo:** una tabla por asignatura (Orientación a Objetos II, Base de Datos, Inteligencia de Negocios I y II, Programación Web II) con el componente del sistema que la evidencia, el archivo y el porqué, más una trazabilidad requerimiento → componente → asignatura.
3. **Manual de usuario por rol** (Administrador, Supervisor, Analista, Cliente), con capturas: qué puede hacer cada uno y cómo.
4. **Ejemplos exportados** en `docs/evaluacion3/ejemplos/` (los de R4) y las capturas de la vista de impresión (A2).
5. **README:** verificar que se pueda instalar y ejecutar con `docker compose up` siguiendo solo el README, y que declare el uso de IA (herramienta y propósito).
6. **Presentación** para la defensa, siguiendo los 7 puntos de la pauta, con un **ensayo cronometrado** de 20 minutos.
7. **`AI_LOG.md`:** cada uno registra sus prompts y lo que aceptó o corrigió.

---

## 5. Lista de verificación final (antes del 3 de diciembre)

- [ ] `main` compila y el CI está en verde (R1).
- [ ] `.gitignore` excluye los `.env` (A0).
- [ ] Dashboard con 6 indicadores, 4 o más gráficos accesibles, filtros y resumen con IA (R3 y A1).
- [ ] 3 reportes con Excel, PDF y CSV, filtros, permisos, auditoría de la exportación e impresión limpia (R4 y A2).
- [ ] Todas las restricciones CHECK en la base (R1).
- [ ] Plan de despliegue con los 10 apartados y la reversión probada (R5).
- [ ] axe sin violaciones en todas las vistas, NVDA documentado, capturas de 360 a 1440 px (A3).
- [ ] La documentación coincide con el código: ADR, ER, diccionario, requerimientos, contrato (A4).
- [ ] Informe técnico en PDF, informe de integración, manual por rol, ejemplos exportados, presentación (A5).
- [ ] `docker compose up` funciona en una máquina limpia siguiendo el README (R6).
- [ ] Cada integrante tiene commits propios y su entrada en `AI_LOG.md`.
