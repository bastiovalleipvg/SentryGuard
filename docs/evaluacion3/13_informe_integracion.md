# 13. Informe de integración del módulo

Este documento dice, por asignatura, **qué componente de SentryGuard la evidencia, dónde está y por qué**, y enlaza cada requerimiento con su componente y su asignatura. Termina con una reflexión sobre las decisiones tomadas.

## 1. Qué evidencia cada asignatura

### Orientación a Objetos II

| Componente | Dónde está | Por qué evidencia la asignatura |
|---|---|---|
| Capas con interfaces (endpoint → servicio → repositorio → `DbContext`) | `SentryGuard/Endpoints`, `Services`, `Repositories`, `Data` | Cada capa depende de abstracciones (`IIncidenteService`, `IRepository<T>`): principio de inversión de dependencias (la D de SOLID) y servicios que se prueban sin la base de datos |
| **Strategy**: exportación de reportes | `Services/Reportes/IExportador.cs` y sus tres implementaciones (`ExportadorExcel`, `ExportadorPdf`, `ExportadorCsv`) | Agregar un formato nuevo no toca el servicio ni el endpoint (principio abierto/cerrado): el endpoint elige la estrategia por el parámetro `formato` |
| **Adapter**: proveedor de IA | `Services/IAsistenteIA.cs`, `AsistenteGemini`, `AsistenteSimulado` | El proveedor se puede cambiar o simular sin tocar los endpoints (Liskov: el simulado reemplaza al real en las pruebas) |
| **Repository** y **DTO** | `Repositories/`, `DTOs/` | La API nunca devuelve entidades de EF: evita ciclos y filtraciones (OWASP API3) |
| **State** como datos: flujo de estados | `tb_TransicionEstado` y `IncidenteService.CambiarEstado` | Las transiciones válidas son datos, no `if` repartidos; una transición inválida da 409 |
| `ServiceResult<T>` | `Common/ServiceResult.cs` | Un único tipo de resultado para éxito y errores; los endpoints solo lo traducen a HTTP |
| Cadena de responsabilidad | `Middleware/` (errores, cabeceras, logs estructurados) | Cada aspecto transversal es un eslabón independiente |
| Pruebas unitarias con dobles | `SentryGuard.Tests/*ServiceUnitTests.cs` | Verifican la lógica aislada de la infraestructura |

### Base de Datos

| Componente | Dónde está | Por qué evidencia la asignatura |
|---|---|---|
| Modelo relacional normalizado y migraciones versionadas | `SentryGuard/Models`, `Migrations/` (6 migraciones) | Esquema reproducible desde cero; las migraciones se revierten (`evidencias/reversion-migracion.md`) |
| **Restricciones de integridad** en la base | 9 CHECK, claves foráneas, únicos y la clave compuesta `tb_Incidente(activo_id, organizacion_id)` | Una regla importante vive en tres niveles: formulario, API y base de datos |
| **Vistas SQL** para el análisis | `vw_IncidenteDetalle`, `vw_CumplimientoSla`, `vw_IndicadoresMensuales` | El dashboard y los reportes leen vistas, no duplican la lógica en C# |
| **Auditoría** por entidad | `tb_LogAuditoria` + `SaveChangesAsync` | Quién cambió qué, con los valores anteriores y nuevos |
| Datos sintéticos reproducibles | `Data/GeneradorDatosSinteticos.cs` (semilla fija, 520 incidentes, idempotente) | Datos coherentes con los estados y los SLA, sin datos personales |
| Respaldo y restauración verificados | `scripts/respaldo-restauracion.ps1`, `evidencias/respaldo-restauracion.txt` | RTO de 30 min y RPO de 24 h, con comparación de filas |
| Concurrencia | `RowVersion` en `tb_Incidente` | Un 409 evita pisar el cambio de otra persona |

### Inteligencia de Negocios I (indicadores y análisis)

| Componente | Dónde está | Por qué evidencia la asignatura |
|---|---|---|
| 6 indicadores con fórmula, variación y pregunta de negocio | `Services/DashboardService.cs`, sección 8 del informe técnico | Cada indicador se define, se calcula en SQL y se interpreta |
| Series para el análisis (tendencia mensual, severidad, categoría, organización, carga por analista) | `GET /api/v1/dashboard/series` | Segmentación y comparación contra el período anterior de igual duración |
| Reportes operativo, gerencial y de SLA | `Services/ReporteService.cs` | Distintos niveles de lectura (detalle, resumen y cumplimiento) para distintas personas |

### Inteligencia de Negocios II (visualización y apoyo a la decisión)

| Componente | Dónde está | Por qué evidencia la asignatura |
|---|---|---|
| Dashboard con 6 gráficos accesibles y filtros interactivos | `sentryguard-web/src/features/dashboard`, `components/Graficos.tsx` | Cada gráfico responde una pregunta y lleva su conclusión escrita |
| Resumen en lenguaje natural con IA sobre cifras agregadas | `POST /api/v1/ia/resumir`, botón «Resumir con IA» | La IA ayuda a leer los datos, pero la persona decide; lleva la etiqueta «Generado con IA» |
| Exportación para otros análisis (Excel, CSV) y para circulación (PDF) | `reportes/{tipo}/exportar` | Los datos salen en el formato que cada audiencia necesita |
| Recomendación de negocio | Sección 8.4 del informe técnico | Cierra el ciclo: dato → interpretación → decisión |

### Programación Web II

| Componente | Dónde está | Por qué evidencia la asignatura |
|---|---|---|
| API REST versionada (`/api/v1`) con contrato OpenAPI | `docs/evaluacion3/contrato-api/openapi-implementado.json` (40 rutas, 62 operaciones) | Contrato único entre el frontend y el backend |
| Seguridad: JWT, roles, aislamiento por organización, rate limiting, CORS, cabeceras | `AuthService`, `Program.cs`, `SecurityHeadersMiddleware` | 13 riesgos del OWASP API Top 10 mitigados y probados |
| Validación en tres niveles y errores estándar (RFC 9457) | `Endpoints/Validacion.cs`, `Common/ApiResults.cs`, Zod en el frontend | Mensajes por campo y un `traceId` para rastrear |
| SPA con React, enrutamiento por rol y estado del servidor | `sentryguard-web/src` | TanStack Query, React Router, React Hook Form |
| Accesibilidad WCAG 2.2 AA y diseño responsivo | `docs/evidencias/axe-reporte.json`, capturas de 320 a 1440 px | axe sin violaciones en 18 vistas; revisión con teclado |
| Pruebas: unitarias, de integración, de componentes y E2E | 115 de backend, 53 de componentes y 5 E2E | La integración se prueba contra SQL Server real |
| Contenerización, CI/CD y despliegue | `compose.yaml`, `.github/workflows/ci.yml`, `12_plan_despliegue.md` | `docker compose up` levanta todo; el pipeline publica solo si pasan las pruebas |

## 2. Trazabilidad: requerimiento → componente → asignatura

| Requerimiento | Componente principal | Asignaturas |
|---|---|---|
| RF-01 a RF-03 (sesión y roles) | `AuthService`, `permisos.ts`, `ProtectedRoute` | Web II |
| RF-04 a RF-10 (incidentes, flujo, bitácora) | `IncidenteService`, `tb_TransicionEstado`, `IncidenteAPI` | OO II, Base de Datos, Web II |
| RF-11 (SLA) | `vw_CumplimientoSla`, `AuthService`/servicio de SLA | Base de Datos, BI I |
| RF-12 a RF-15 (mantenedores) | `ActivoService`, `OrganizacionService`, `CatalogoAPI` | OO II, Base de Datos |
| RF-16 (auditoría) | `SaveChangesAsync`, `tb_LogAuditoria` | Base de Datos |
| RF-17 (dashboard) | `DashboardService`, `DashboardPage` | BI I, BI II, Web II |
| RF-18 a RF-20 (3 reportes) | `ReporteService`, `ReportesPage` | BI I, BI II |
| RF-21 (exportación) | `IExportador` y 3 estrategias | OO II, BI II |
| RF-22 (impresión) | `@media print` en `global.css` | Web II |
| RF-23 y RF-24 (IA) | `IAsistenteIA`, `iaService.ts` | OO II, BI II, Web II |
| RF-25 (datos sintéticos) | `GeneradorDatosSinteticos` | Base de Datos, BI I |
| RNF-02 y RNF-03 (seguridad) | JWT, rate limiting, aislamiento por organización | Web II |
| RNF-04, RNF-05, RNF-09, RNF-12 (operación) | `compose.yaml`, respaldo, CI | Base de Datos, Web II |
| RNF-06 y RNF-07 (accesibilidad, responsivo) | `FormField`, `ListadoResponsivo`, axe | Web II |
| RNF-08 (mantenibilidad) | Capa de servicios con interfaces | OO II |
| RNF-11 (auditabilidad) | Auditoría de cambios y de exportaciones | Base de Datos |

## 3. Reflexión crítica sobre las decisiones

- **Una sola base de código en lugar de dos.** Partimos del proyecto de Adriel y no del de Bastián porque ya tenía las pruebas, los roles y el frontend por *features*. El costo fue integrar a mano lo de `SentryGuard-2`, y dejó una rotura temporal de la compilación (definiciones duplicadas de la categoría y la auditoría) que se resolvió el 22 de noviembre. **Lección:** integrar con ramas y *pull requests* con el CI en verde, no directamente sobre `main`.
- **Gemini en vez de Anthropic (ADR-007).** Elegimos por costo, y el patrón Adapter hizo barato el cambio. La documentación del Hito 1 decía otra cosa; la corregimos porque la pauta penaliza esa diferencia.
- **Vistas SQL para el análisis.** Hacen simple el servicio, pero parte de la lógica vive en la migración. Aceptamos ese costo porque el dashboard se calcula en la base y se mantiene la fórmula en un solo lugar.
- **Gráficos SVG propios en lugar de una librería.** Pesan menos y controlamos la accesibilidad (tabla de datos y conclusión escrita), pero solo hay tres tipos de gráfico.
- **Cálculo parcial en memoria.** Las series mensuales y el cumplimiento de SLA se agrupan en C# después de traer las filas del período. Con 520 incidentes es instantáneo; con millones habría que mover la agregación a SQL (mejora futura).
- **Sin sistema de alertas ni alta disponibilidad.** Lo declaramos como limitación en el plan de despliegue.
