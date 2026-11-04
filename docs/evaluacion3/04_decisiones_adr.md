# 4. Registro de decisiones de arquitectura (ADR)

Cada decisión indica su contexto, la opción elegida, las alternativas descartadas y sus consecuencias. **Estado:** *Aceptada* (ya aplicada en el código) o *Propuesta* (se aplica en el hito indicado; puede ajustarse con la retroalimentación del docente).

## ADR-001 · Base del proyecto: el repositorio SentryGuard original
- **Estado:** Aceptada (Hito 1).
- **Contexto:** Después del Trabajo 1 existen dos versiones: `SentryGuard` (original, que continuó Adriel) y `SentryGuard-2` (fork de Bastián para el Trabajo 2).
- **Decisión:** Usar `SentryGuard` como base, porque tiene más módulos de negocio (organizaciones, contratos SLA, catálogos, bitácora y evidencias), validación más completa y evidencia de accesibilidad en 15 vistas. Del fork se incorporan su formato de error, su dashboard, Dependabot y su tema oscuro (ver [INTEGRACION_SENTRYGUARD2.md](INTEGRACION_SENTRYGUARD2.md)).
- **Alternativa descartada:** Usar `SentryGuard-2`. Tiene un historial más limpio y equilibrado, pero habría que rehacer varios módulos, lo que costaría entre una y dos de las cinco semanas.
- **Consecuencias:** Hay que limpiar una clave JWT antigua del historial (ADR-011), y el aporte de Bastián debe verse en los commits desde el Hito 2.

## ADR-002 · Arquitectura en capas con servicios y Repository
- **Estado:** Propuesta (Hito 2).
- **Contexto:** Hoy la lógica de negocio está en los endpoints, que usan el `DbContext` directamente.
- **Decisión:** Mover las reglas a servicios con interfaz y usar los repositorios desde los servicios. Los endpoints quedan como adaptadores HTTP.
- **Alternativas descartadas:**
  - *Dejar la lógica en los endpoints*: la pauta califica esto como «En desarrollo» y no permite pruebas unitarias.
  - *Usar el `DbContext` directo en los servicios, sin Repository*: es válido con EF Core, pero la pauta exige Repository.
  - *Arquitectura limpia con varios proyectos*: es demasiado para cinco semanas.
- **Consecuencias:** El refactor se hace protegido por las 24 pruebas de integración existentes. Las consultas de reportes usan vistas en lugar de repositorios genéricos.

## ADR-003 · JWT en `sessionStorage` y Bearer en la cabecera
- **Estado:** Aceptada (Trabajo 2).
- **Decisión:** Guardar el token en `sessionStorage` y enviarlo en `Authorization: Bearer`.
- **Alternativas descartadas:**
  - *`localStorage`*: persiste y se comparte entre pestañas.
  - *Solo en memoria*: se pierde al recargar.
  - *Cookie `HttpOnly`*: exige rehacer la autenticación y agregar protección CSRF.
- **Consecuencias:** El riesgo de XSS se mitiga con una CSP estricta, React escapando todo el contenido y un token de vida corta. El detalle está en `docs/INFORME_TECNICO.md` §5.

## ADR-004 · SQL Server 2022 en contenedor
- **Estado:** Propuesta (Hito 3).
- **Decisión:** Mantener SQL Server usando la imagen oficial en compose.
- **Alternativa descartada:** Migrar a PostgreSQL. Es más liviano, pero obliga a rehacer las migraciones y a revisar las consultas a dos semanas del cierre.
- **Consecuencias:** Requiere unos 2 GB de RAM y no tiene imagen ARM, lo que afecta a los Mac con chip Apple. Se documenta como requisito.

## ADR-005 · Reportes generados en el backend
- **Estado:** Propuesta (Hito 4).
- **Decisión:** Generar Excel con ClosedXML, PDF con QuestPDF y CSV con la biblioteca estándar, todo en la API, detrás de la interfaz `IExportador` (patrón Strategy).
- **Alternativa descartada:** Generar los archivos en el navegador (SheetJS, jsPDF). Es más simple, pero la API no podría garantizar los permisos por rol ni registrar quién exportó, que es lo que pide la pauta.
- **Consecuencias:** Hay dos dependencias nuevas. Antes de agregarlas se revisa la licencia de QuestPDF [NO VERIFICADO: los términos de la licencia Community vigentes]. Las exportaciones grandes se generan en *streaming* para no bloquear la interfaz.

## ADR-006 · Dashboard con endpoints de agregación y vistas SQL
- **Estado:** Propuesta (Hito 4).
- **Decisión:** La API expone `/dashboard/*`, que consulta vistas SQL de agregación. El frontend dibuja los gráficos como componentes SVG propios y accesibles, a partir de los de SentryGuard-2.
- **Alternativas descartadas:**
  - *Power BI o Metabase conectado a la base*: queda fuera del sistema, no respeta los roles de la API y complica `docker compose up`.
  - *Una librería de gráficos genérica*: suma peso y hay que adaptar su accesibilidad.
- **Consecuencias:** Cada gráfico tiene título, una descripción textual y una tabla con sus datos. Los filtros se envían como parámetros de consulta.

## ADR-007 · IA detrás de un adaptador, con la API de Anthropic
- **Estado:** Propuesta (Hito 3).
- **Decisión:** Crear la interfaz `IAsistenteIA` con dos implementaciones: `AsistenteAnthropic` (la real, con `HttpClient` tipado y *timeout* de 15 s) y `AsistenteFalso` (para las pruebas y para CI). El modelo se configura con `IA__Modelo`; se propone `claude-haiku-5-5`, porque ambas tareas son cortas y conviene la velocidad. La clave va en `IA__ApiKey`, nunca en el repositorio.
- **Alternativas descartadas:**
  - *Llamar a la IA desde el navegador*: expondría la clave.
  - *Otro proveedor*: es posible gracias al adaptador, pero no se evalúa.
- **Consecuencias:**
  - Las respuestas de la IA se validan como JSON contra un esquema: la severidad y la categoría deben existir en los catálogos.
  - Si fallan, la API responde 502 o 504 con el formato estándar.
  - Solo se envía el texto del incidente o los indicadores agregados, nunca datos de usuarios.
  - La sugerencia nunca se aplica sola: la persona la confirma.

## ADR-008 · Flujo de estados como tabla de transiciones
- **Estado:** Propuesta (Hito 2).
- **Decisión:** Crear una tabla `tb_TransicionEstado (estado_origen_id, estado_destino_id)` con un índice único y la restricción CHECK `origen <> destino`. `IIncidenteService.CambiarEstado` rechaza con 409 lo que no esté en la tabla.
- **Alternativa descartada:** Un `switch` en el código. Es más simple, pero las reglas no quedan en el modelo de datos ni se pueden consultar.
- **Consecuencias:** La interfaz pide los estados permitidos para el estado actual y solo ofrece esos.

## ADR-009 · Datos sintéticos con un generador de semilla fija
- **Estado:** Propuesta (Hito 2).
- **Decisión:** Un generador en C# con semilla fija, idempotente (si ya hay datos, no hace nada), que crea al menos 500 incidentes repartidos en 12 meses, con su bitácora, sus asignaciones y sus cierres coherentes con los estados. Se ejecuta en Development y en el contenedor `seed`.
- **Alternativas descartadas:**
  - *Poner 500 filas en una migración con `HasData`*: la migración queda enorme y mezcla esquema con datos de demostración.
  - *Un script SQL a mano*: es difícil mantener la coherencia entre las tablas.
- **Consecuencias:** Los datos son reproducibles, así que los indicadores del informe se pueden recalcular. Ningún dato corresponde a personas reales.

## ADR-010 · Auditoría por entidad con un interceptor de EF Core
- **Estado:** Propuesta (Hito 2).
- **Decisión:** Agregar a `tb_LogAuditoria` las columnas `entidad`, `entidad_id`, `operacion` y `detalle` (JSON con los valores anteriores y nuevos). Un `SaveChangesInterceptor` las llena en cada cambio. El middleware actual sigue registrando las peticiones de escritura.
- **Alternativa descartada:** Agregar las columnas `creado_por`, `modificado_por` y sus fechas a cada tabla. Solo guarda el último cambio y no qué cambió.
- **Consecuencias:** Requiere una migración y un índice por (`entidad`, `entidad_id`). Las exportaciones se registran con `operacion = EXPORTAR`.

## ADR-011 · Limpieza de una clave antigua del historial de git
- **Estado:** Propuesta (antes del Hito 2).
- **Contexto:** El commit `14d8e52` (Trabajo 1) contiene una clave JWT de desarrollo en `appsettings.json`. Ya se rotó: la clave actual es distinta, como se comprobó comparando hashes. La pauta dice que una credencial expuesta en el historial deja el criterio en Insuficiente.
- **Decisión:** Reescribir el historial con `git filter-repo` para reemplazar esa clave, respaldar el repositorio antes y hacer un *force-push* coordinado con todo el equipo.
- **Alternativa descartada:** Solo documentar que se rotó. Es menos riesgoso, pero queda a criterio del docente.
- **Consecuencias:** Cambian los hashes de todos los commits, así que cada integrante debe volver a clonar el repositorio. Las referencias a commits en los documentos del Trabajo 2 dejarán de coincidir, y se actualizan.
