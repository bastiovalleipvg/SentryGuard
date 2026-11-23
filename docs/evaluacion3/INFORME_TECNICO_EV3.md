# Informe técnico: SentryGuard (Evaluación 3)

**Asignatura:** Lenguaje de Programación Web II · **Equipo:** Adriel Troncoso, Bastián Ovalle y Rafael Navarrete · **Fecha:** 23 de noviembre de 2026

> Este informe resume el sistema y remite a los documentos de `docs/evaluacion3/` para el detalle. Todo el dominio es ficticio y los datos son sintéticos. **Pendiente de la persona antes de entregar:** registrar la aprobación del docente (Hito 1) y completar el recorrido con NVDA (sección 7).

## 1. Introducción y dominio

CyberOps Chile SpA (empresa ficticia) presta ciberseguridad gestionada a varias organizaciones. Sin un sistema común, los incidentes llegan por correo o teléfono, no hay un registro único, el supervisor no ve qué incidente está por vencer su SLA y la gerencia no tiene indicadores.

**Objetivo:** un sistema web para registrar, asignar, atender y cerrar incidentes con trazabilidad y control del SLA, que además entregue un dashboard y reportes exportables, y ayude con IA a clasificar incidentes y resumir indicadores.

**Alcance:** 4 roles (Administrador, Supervisor, Analista, Cliente); incidentes con bitácora y evidencias; activos, organizaciones y contratos SLA; auditoría; dashboard con 6 indicadores; 3 reportes con exportación a Excel, PDF y CSV e impresión; 2 funciones de IA. **Fuera de alcance:** notificaciones por correo, integración con herramientas de monitoreo reales y alta disponibilidad. Detalle en `01_propuesta_dominio.md`.

## 2. Plan de trabajo

| Integrante | Área principal |
|---|---|
| Adriel Troncoso | Frontend (pantallas, dashboard, reportes e impresión), accesibilidad y documentación |
| Bastián Ovalle | Backend y datos: capa de servicios, migraciones, datos sintéticos, auditoría |
| Rafael Navarrete | Contenerización y CI/CD, restricciones de base de datos, endpoints de dashboard y reportes |

Hitos: 1 (propuesta, 5 nov.), 2 (datos y backend), 3 (frontend, contenedores e IA), 4 (dashboard, reportes y despliegue) y entrega el 3 de diciembre. Herramientas: Git y GitHub (con Dependabot), .NET 10, SQL Server 2022, React 19, Docker y GitHub Actions. El repositorio es un monorepo: `SentryGuard/` (API), `SentryGuard.Tests/`, `sentryguard-web/`, `docs/`, `scripts/`. Los commits son en español con prefijo (`feat:`, `fix:`, `docs:`, `test:`, `chore:`). Detalle en `08_plan_trabajo.md`. El uso de IA de apoyo (Claude Code, y las demás herramientas que usó cada integrante) está declarado en el `README.md` y en `AI_LOG.md`.

## 3. Requerimientos

25 requerimientos funcionales (RF-01 a RF-25), 12 no funcionales (RNF-01 a RNF-12) y 13 historias de usuario priorizadas con el método MoSCoW (M/S), con una matriz de permisos por rol. Los más importantes: sesión con JWT de 60 minutos y roles (RF-01 a RF-03); incidentes con flujo de estados válido (RF-04 a RF-10); SLA (RF-11); auditoría por entidad (RF-16); dashboard (RF-17); tres reportes y su exportación (RF-18 a RF-22); IA (RF-23, RF-24); accesibilidad WCAG 2.2 AA (RNF-06) y seguridad (RNF-02, RNF-03). Cada uno tiene su estado y su forma de verificación en `02_requerimientos_historias.md`.

## 4. Arquitectura de software

**Capas:** endpoint delgado → servicio (interfaz, devuelve `ServiceResult<T>`) → repositorio → `DbContext` → SQL Server. El frontend es una SPA por *features* que solo habla con la API por un cliente HTTP único. Diagramas en `diagramas/` (`componentes`, `despliegue`, `flujo-estados-incidente`).

**Patrones aplicados:** Repository, inyección de dependencias, cadena de middleware, DTO y proyección, **Strategy** (exportadores), **Adapter** (IA), **State como datos** (tabla de transiciones) y `ServiceResult`.

**Decisiones:** 11 ADR en `04_decisiones_adr.md`, entre ellas ADR-007 (IA detrás de un adaptador, con Gemini), ADR-008 (flujo de estados como tabla), ADR-009 (datos sintéticos con semilla fija), ADR-010 (auditoría dentro de `SaveChangesAsync`) y ADR-011 (limpieza de una clave del historial de git). Detalle en `03_arquitectura.md`.

## 5. Modelo de datos

El diagrama entidad–relación y el diccionario de datos están en `05_modelo_datos.md` y `diagramas/er-actual.*`. Hay 6 migraciones (de `InicialSentryGuard` a `AgregaRestriccionesCheck`), 9 restricciones CHECK, una clave foránea compuesta que impide asociar un incidente a un activo de otra organización, `RowVersion` para concurrencia y 3 vistas SQL (`vw_IncidenteDetalle`, `vw_CumplimientoSla`, `vw_IndicadoresMensuales`).

**Datos de prueba:** generador con semilla fija e idempotente (520 incidentes en 12 meses, con bitácora y cierres coherentes).

**Respaldo:** `scripts/respaldo-restauracion.ps1` hace un `BACKUP` con `CHECKSUM`, lo verifica, lo restaura con otro nombre y compara las filas; la evidencia está en `docs/evidencias/respaldo-restauracion.txt`. **Reversión de migraciones** probada: `evidencias/reversion-migracion.md`.

## 6. Backend y API

- **Contrato:** `contrato-api/openapi-evaluacion3.yaml` (diseño) y `openapi-implementado.json` (exportado de la API en ejecución: 40 rutas y 62 operaciones). Documentación interactiva en `/scalar/v1` con el esquema Bearer.
- **Seguridad:** JWT, hash de contraseñas (PBKDF2), bloqueo de la cuenta tras 5 intentos, RBAC en cada endpoint de escritura, **aislamiento por organización y por asignación** (el ID de la URL nunca se confía), *rate limiting*, CORS restringido, cabeceras de seguridad y errores genéricos sin enumeración de usuarios. 13 riesgos del OWASP API Top 10 mitigados (`docs/INFORME_TECNICO.md` §7 y `evidencias/seguridad-api.txt`).
- **Validaciones:** en tres niveles (Zod, `Validacion.cs` y restricciones de la base) y errores con el formato ProblemDetails (RFC 9457) y un `codigo`.
- **Pruebas:** **115 pruebas de backend** (unitarias de los servicios e integración con `WebApplicationFactory` contra SQL Server) en verde, que cubren los códigos 401, 403, 404, 409 y 400, el aislamiento entre organizaciones, el dashboard y los reportes (permisos, filtros y los tres formatos de exportación). Reporte: `evidencias/pruebas-backend.txt`.

## 7. Front end y accesibilidad

- **Guía de estilos y maquetas:** `07_maquetas_guia_estilos.md` y `maquetas/`. Variables CSS (`--primario`, `--texto`...), mobile first, objetivos táctiles de 44 px.
- **Responsive:** capturas a 320, 360, 390, 768, 1280 y 1440 px de las vistas principales en `docs/evidencias/capturas/`, sin desplazamiento horizontal en ningún ancho.
- **Accesibilidad automática:** **axe-core con las etiquetas WCAG 2.2 AA: 0 violaciones en 18 vistas** (`evidencias/axe-reporte.json`), más `oxlint` con `jsx-a11y`. **Lighthouse no se ejecutó**: se usó axe, que la pauta acepta.
- **Teclado:** recorrido documentado en `evidencias/revision-manual-accesibilidad.md` (enlace para saltar al contenido, foco gestionado, diálogos con `Escape`).
- **Lector de pantalla:** **pendiente a mano.** La tabla de pasos para NVDA está lista en el mismo documento (sección 6) y debe completarla una persona.
- **Pruebas:** 53 de componentes (Vitest, Testing Library y MSW) y 5 E2E con Playwright contra la API real.

## 8. Análisis de datos, dashboard y reportes

### 8.1 Indicadores

Valores reales obtenidos con el dato sintético cargado, del 1 de enero al 23 de noviembre de 2026 (`ejemplos/indicadores.json`):

| Indicador | Fórmula | Pregunta de negocio | Valor | Interpretación |
|---|---|---|---|---|
| Volumen de incidentes | cantidad creada en el período | ¿Cuánta demanda hay? | 468 | Unos 42 por mes con un pico de 61 en octubre; hay que dimensionar el equipo para ese pico, no para el promedio |
| Cumplimiento del SLA | cerrados dentro del plazo / cerrados × 100 | ¿Cumplimos lo prometido? | 83,77 % | Sobre el período anterior (+8,3 pp), pero 5 de los 11 meses están bajo el 80 % (el último es parcial) |
| Tiempo medio de resolución | promedio de horas de creación a cierre | ¿Cuánto tardamos? | 10,05 h | Mejora de 1,02 h respecto del período anterior |
| Tiempo medio de asignación | promedio de horas de creación a «Analista asignado» | ¿Qué tan rápido entra un incidente a un analista? | 0,74 h | Rápido (menos de una hora), pero subió 0,49 h: vigilar |
| Backlog activo | incidentes sin Resolver ni Cerrar al final del período | ¿Cuánto trabajo pendiente hay? | 49 | Equivale a unos 1,2 meses de demanda; la carga está concentrada en pocos analistas (ver 8.3) |
| Proporción de críticos | críticos / total × 100 | ¿Qué parte del trabajo es urgente? | 9,62 % | Casi 1 de cada 10; sube 2,07 pp |

(El período anterior de igual duración tiene pocos datos sintéticos, por eso algunas variaciones, como la del volumen, son grandes: con datos reales se interpretarían con más cuidado.)

### 8.2 Visualizaciones

Seis gráficos accesibles: incidentes por mes (barras), cumplimiento del SLA por mes (línea), por severidad, por categoría, por organización y carga abierta por analista (barras horizontales). Cada uno lleva su conclusión escrita y una tabla con los mismos datos. Filtros: fechas, organización, severidad y categoría, guardados en la URL.

### 8.3 Lectura de las series

- Categoría dominante: **«Acceso no autorizado» con 228 de 468 incidentes (49 %)**.
- Severidad: 183 Media, 134 Baja, 106 Alta y 45 Crítica.
- Carga abierta: Analista Demo 21, 14 sin asignar, Carlos Mendoza 9 y Valeria Lagos 5.

### 8.4 Recomendación de negocio

1. **Reforzar la prevención de acceso no autorizado** (autenticación multifactor y revisión de cuentas), porque es casi la mitad de la demanda.
2. **Rebalancear la carga**: el 43 % de los incidentes abiertos recae en una sola persona y hay 14 sin asignar; el Supervisor debería asignarlos hoy.
3. **Revisar los meses bajo el 80 % de SLA** (enero, abril, mayo y junio; noviembre aún está incompleto) para ver si coinciden con licencias o con las severidades altas, y ajustar la dotación o los plazos del contrato.

### 8.5 Reportes y exportación

| Reporte | Contenido |
|---|---|
| Operativo | Detalle de cada incidente, con filtros, orden y paginación |
| Gerencial | Totales por organización, severidad y estado |
| SLA | Cumplimiento y horas de resolución por mes; exige un rango de fechas |

Cada uno se exporta a **Excel** (encabezados, formatos y fila de totales), **PDF** (título, filtros, fecha y «Página X de Y») y **CSV** (UTF-8 con BOM y separador `;`). Respetan el rol (Cliente: su organización; Analista: sin acceso), se limitan a 20 exportaciones por minuto y **cada exportación queda registrada en la auditoría**. Archivos de ejemplo en `ejemplos/`. La vista de impresión oculta el menú, los filtros y los botones.

## 9. Integración de IA

- **Servicio:** Gemini (`gemini-2.5-flash`), detrás de la interfaz `IAsistenteIA` (ADR-007). Sin clave, la API usa un asistente simulado.
- **Endpoints:** `POST /api/v1/ia/clasificar-incidente` (sugiere severidad y categoría que la persona confirma) y `POST /api/v1/ia/resumir` (resumen del dashboard, solo Administrador y Supervisor, con la etiqueta «Generado con IA»).
- **Clave:** en `IA__ApiKey` (variable de entorno), enviada en el encabezado `x-goog-api-key`, **nunca en la URL ni en el repositorio**. Solo se envía el texto del incidente o cifras agregadas.
- **Errores:** tiempo máximo de 15 s; si el proveedor falla o tarda, la API responde 502 (`IA_ERROR_PROVEEDOR`) o 504 (`IA_TIEMPO_AGOTADO`) con el formato estándar y la interfaz sigue funcionando sin la sugerencia. Probado en `IaApiTests.cs` y `dashboardReportes.test.tsx`. Límite de 15 llamadas por minuto.

## 10. Contenerización y despliegue

- **Dockerfiles** multi-stage con versión fija (API .NET 10 y web con nginx), `compose.yaml` con `db`, `api`, `seed` y `web`, redes `app` y `datos`, `healthcheck`, `restart: unless-stopped`, contenedores en modo solo lectura y sin privilegios. **Un único comando:** `docker compose up --build` con `.env` generado por `scripts/generar-env.*`.
- **Pipeline** (`.github/workflows/ci.yml`): backend con SQL Server, frontend (lint, tipos, pruebas, build y `npm audit`), construcción de contenedores, escaneo Trivy y **publicación en GHCR solo desde `main` y solo si todo pasó**. Dependabot propone actualizaciones.
- **Plan de despliegue:** 10 apartados en `12_plan_despliegue.md` (entornos, infraestructura, pasos, secretos, reversión probada, respaldo con RTO de 30 min y RPO de 24 h, monitoreo, seguridad, costos y riesgos, y lista de verificación). Los precios están marcados como no verificados.

## 11. Articulación del módulo y cierre

**Qué componente evidencia cada asignatura:** ver `13_informe_integracion.md` (tablas por asignatura y trazabilidad requerimiento → componente → asignatura).

**Aporte de cada integrante:** ver el plan de trabajo y el historial de commits (`git shortlog -sne`).

**Limitaciones:**
- Sin sistema de alertas ni alta disponibilidad.
- La agrupación mensual del dashboard se hace en C# sobre las filas del período (suficiente para cientos o miles de incidentes).
- Solo hay tres tipos de gráfico propios.
- Plan de despliegue sin probar en un servidor con dominio y HTTPS reales, ni la reversión de imágenes con Docker.
- No se midió la carga (RNF-01 se verificó a mano con 520 incidentes).
- Licencia de QuestPDF por verificar para un uso comercial.

**Mejoras futuras:** notificaciones por correo al acercarse el vencimiento del SLA, agregaciones en SQL para volúmenes grandes, alertas de monitoreo, autenticación de dos factores y una revisión completa con lectores de pantalla en móvil.

**Lecciones aprendidas:** integrar con ramas y con el CI en verde (una integración directa en `main` dejó el proyecto sin compilar varios días); mantener la documentación junto al código y revisarla contra lo implementado; y que las pruebas de integración contra una base real detectan errores (como una restricción duplicada entre migraciones) que las unitarias no ven.
