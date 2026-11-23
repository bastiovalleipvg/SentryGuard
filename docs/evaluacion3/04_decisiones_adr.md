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

## ADR-007 · IA detrás de un adaptador, con la API de Gemini
- **Estado:** Aceptada (implementada en el Hito 3).
- **Decisión:** La interfaz `IAsistenteIA` tiene dos implementaciones: `AsistenteGemini` (la real, con `HttpClient` tipado, *timeout* de 15 s y la clave en el encabezado `x-goog-api-key`) y `AsistenteSimulado` (heurístico, para las pruebas, para CI y cuando no hay clave). El modelo se configura con `IA__Modelo` (por defecto `gemini-2.5-flash`) y la clave con `IA__ApiKey`; nunca van en el repositorio.
- **Por qué Gemini y no Anthropic (propuesta inicial):** el equipo contaba con una clave gratuita de Gemini para desarrollar y demostrar sin costo. El patrón Adapter permite cambiar de proveedor escribiendo una clase nueva, sin tocar los endpoints.
- **Alternativas descartadas:**
  - *Llamar a la IA desde el navegador*: expondría la clave.
  - *Anthropic*: era la propuesta del Hito 1; se descartó por costo en esta etapa.
- **Consecuencias:**
  - La respuesta de la IA se valida como JSON: la severidad y la categoría deben existir en los catálogos.
  - Si el proveedor falla o tarda más de 15 s, la API responde 502 o 504 con el formato estándar (`IA_ERROR_PROVEEDOR`, `IA_TIEMPO_AGOTADO`) y el formulario sigue funcionando sin la sugerencia.
  - Solo se envía el texto del incidente o cifras agregadas, nunca datos de usuarios.
  - La sugerencia nunca se aplica sola: la persona la confirma. El resumen del dashboard lleva la etiqueta «Generado con IA».

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

## ADR-010 · Auditoría por entidad dentro de `SaveChangesAsync`
- **Estado:** Aceptada (implementada en el Hito 2).
- **Decisión:** `tb_LogAuditoria` tiene las columnas `accion`, `entidad`, `entidad_id`, `valores_anteriores` y `valores_nuevos` (JSON). `SentryGuardDbContext.SaveChangesAsync` recorre los cambios del `ChangeTracker` y registra `CREAR`, `MODIFICAR` o `ELIMINAR` con el usuario, la IP y la hora. En las altas, el id se lee después del primer guardado porque lo genera la base. Se ignoran `RowVersion` y los campos de control. El middleware de auditoría sigue registrando las peticiones de escritura.
- **Por qué no un `SaveChangesInterceptor` (propuesta inicial):** el equipo lo resolvió con una sobrescritura del contexto, que es más simple de probar con el mismo `DbContext` y tiene el mismo efecto.
- **Alternativa descartada:** agregar `creado_por` y `modificado_por` a cada tabla: solo guarda el último cambio, no qué cambió. (Se mantienen además en `tb_Incidente`.)
- **Consecuencias:** las exportaciones de reportes se registran con `accion = EXPORTAR Reporte` y el detalle (tipo, formato y filtros) en `valores_nuevos`.

## ADR-011 · Limpieza de una clave antigua del historial de git
- **Estado:** Aceptada (aplicada el 4 de noviembre de 2026).
- **Contexto:** Un commit del Trabajo 1 (hoy `273f167`, «feat: endpoints CRUD, autenticacion y configuracion de la app») contenía una clave JWT de desarrollo en `appsettings.json`, hasta que se vació en `93e86aa`. Ya estaba rotada: la clave actual es distinta, como se comprobó comparando hashes. La pauta dice que una credencial expuesta en el historial deja el criterio en Insuficiente.
- **Decisión:** Reescribir el historial con `git filter-repo --replace-text`, reemplazando la clave por un valor vacío en los 10 commits donde aparecía. Antes se respaldó el repositorio en un *bundle* de git y después se hizo un *force-push* coordinado con todo el equipo.
- **Verificación:** ningún commit contiene la clave (`git log --all -S`), y el contenido del último commit es idéntico al anterior a la limpieza.
- **Alternativa descartada:** Solo documentar que se rotó. Es menos riesgoso, pero queda a criterio del docente.
- **Consecuencias:** Cambiaron los hashes de todos los commits, así que cada integrante debe volver a clonar el repositorio. Las referencias a commits en `docs/CAMBIOS_API_TRABAJO1.md` se actualizaron. Es posible que GitHub conserve por un tiempo los commits antiguos sin referencia; como la clave ya estaba rotada, no da acceso a nada.
