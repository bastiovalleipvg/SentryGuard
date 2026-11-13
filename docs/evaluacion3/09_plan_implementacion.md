# 9. Plan de implementación (estado al 13 de noviembre de 2026)

Contraste entre la rúbrica de la Evaluación 3, el estado real del repositorio (commit `47ace1b`) y los pendientes que indicó Rafael.
Verificado: `dotnet build` sin errores y 19 pruebas unitarias en verde. **No verificado:** `docker compose up`, el CI en GitHub, las pruebas de integración (requieren SQL Server) y el frontend en esta sesión.

## 9.1 Estado por hito

| Hito | Estado | Detalle |
|---|---|---|
| 1. Propuesta y diseño | Casi completo | `docs/evaluacion3/` está. Faltan registrar la aprobación del docente, confirmar el tercer integrante y las fechas (README de `docs/evaluacion3`). |
| 2. Datos y backend | **Incompleto (~60%)** | Hecho: JWT, roles, ProblemDetails, capa de servicios, pruebas unitarias, generador de 520 incidentes, transiciones validadas en código. Falta lo de la sección 9.2. |
| 3. Front, contenedores e IA | Avanzado, sin verificar (~75%) | Hecho: endpoints de IA con adaptador y simulado, `/health`, Dockerfiles, `compose.yaml`, CI, sugerencia de IA en el formulario. Falta lo de la sección 9.3. |
| 4. Dashboard, reportes, despliegue | No iniciado | Fuera del alcance de hoy, pero pesa 24 de 100 puntos (sección 9.5). |

## 9.2 Lo que dijo Rafael, contrastado con el código

| Punto de Rafael | Estado real | Evidencia |
|---|---|---|
| `tb_CategoriaIncidente` con FK a incidentes | **No existe** | No hay modelo, migración ni referencia en el código. Solo está planificada en `05_modelo_datos.md` §5.4. |
| `tb_TransicionEstado` con la matriz ADR-008 | **No existe como tabla** | La matriz vive en un `Dictionary` dentro de `IncidenteService.cs` y en `transiciones.ts`. Hay que pasarla a datos (y que ambos lados la lean). |
| Auditoría `fecha_creacion`, `fecha_modificacion`, `ip_origen` | **Parcial** | `tb_LogAuditoria` tiene `ip_origen` y `timestamp`. `tb_Incidente` solo tiene `fecha_creacion`; no hay `fecha_modificacion` ni quién modificó. `tb_Activo` hay que revisarla. RF-16 pide valores anteriores y nuevos. |
| Vistas SQL de reportería | **No existen** | Planificadas: `vw_IncidenteDetalle`, `vw_IndicadoresMensuales`, `vw_CumplimientoSla`. |
| Respaldo `.bak` y restauración probada | **No hecho** | Solo está descrito en `05_modelo_datos.md` §5.6. El volumen `sqlbackups` ya existe en `compose.yaml`. |
| "Está todo en el repo" | **No es cierto** | Además de lo anterior, `SentryGuard_Setup.sql` es el script del Trabajo 1 y no refleja las migraciones actuales. |

## 9.3 Otros hallazgos que no mencionó Rafael

1. **Versión de .NET inconsistente.** El proyecto, el Dockerfile y el CI quedaron en .NET 9 (commit `844061e` bajó la versión), pero la documentación dice .NET 10. Decidir una y alinear código, Dockerfile, CI y documentos.
2. **Paquete con vulnerabilidad alta** (`System.Security.Cryptography.Pkcs` 9.0.2, GHSA-846f-f764-76mx). El build lo advierte. En .NET 9 conviene fijar una versión parchada.
3. **Secretos en el repositorio.** `.env.example`, `compose.yaml` y valores por defecto contienen la contraseña de `sa` y una clave JWT reales de desarrollo. La rúbrica baja a Insuficiente el criterio si hay credenciales reales expuestas. Quitar los valores por defecto del compose y dejar placeholders en `.env.example`.
4. **El compose no es verificable aún.** `seed` depende de que la API ya haya migrado la base, y no se ve que la API aplique migraciones al arrancar. Falta confirmar que `docker compose up` deja el sistema usable (migraciones, seed, web con proxy a la API).
5. **CI incompleto frente a la rúbrica:** solo ejecuta las pruebas unitarias; no corre las de integración, no construye las imágenes Docker ni "publica solo si pasan". Falta un job de build/push de imagen condicionado a las pruebas.
6. **Dockerfile y compose sin lo de "Destacado":** imágenes con etiqueta flotante (`nginx:alpine`, `2022-latest`) en vez de versión fija; la web no tiene health check ni usuario no root; faltan redes explícitas.
7. **Historial de commits desbalanceado:** Adritcs 33, Rafael-Ano 12, Bastián 2. La rúbrica ajusta la nota individual por aporte. Cada integrante debe commitear su área.
8. **README desactualizado:** describe solo la ejecución local (`iniciar.bat`), no `docker compose up`, y no declara el uso de IA con el detalle de la Evaluación 3 ni el recuento de pruebas actual.
9. **Cobertura de pruebas de IA:** hay pruebas unitarias con el simulado; falta una prueba de integración del endpoint `/api/v1/ia/*` (Destacado pide pruebas con el servicio simulado).

## 9.3 bis. Avance de la Fase A (13 de noviembre de 2026)

| Paso | Estado |
|---|---|
| 1. Categoría de incidente y `tb_TransicionEstado` | Hecho: migración `AuditoriaCategoriaTransicionesVistas` |
| 2. Auditoría (`fecha_modificacion`, creado y modificado por, detalle por entidad con valores anteriores y nuevos) | Hecho en `tb_Incidente` y `tb_Activo`; el detalle por entidad queda en `tb_LogAuditoria` |
| 3. CHECK y coherencia organización-activo | Parcial: `fecha_cierre >= fecha_creacion` y la FK compuesta. Faltan los demás CHECK de `05_modelo_datos.md` §5.4 |
| 4. Vistas | Hecho: `vw_IncidenteDetalle`, `vw_CumplimientoSla`, `vw_IndicadoresMensuales`, con prueba |
| 5. Servicio de transiciones | Hecho: la API y la interfaz leen `GET /api/v1/catalogos/transiciones-estado` |
| 6. Categoría en API e interfaz | Hecho en crear, editar, filtro de la API y la sugerencia de IA. Falta el filtro y la columna en la interfaz |
| 7. Respaldo y restauración | Hecho y probado: `scripts/respaldo-restauracion.ps1`, evidencia en `docs/evidencias/respaldo-restauracion.txt` |
| 8. Documentación | `05_modelo_datos.md` actualizado a mano; el diagrama ER y el diccionario no se regeneraron |

**Hallazgo extra:** el volumen del `compose.yaml` sobre `/var/opt/mssql/data` impedía arrancar SQL Server (permisos). Se montó sobre `/var/opt/mssql` y se publicó el puerto solo en localhost (`14333`).

**Pruebas:** 51 de integración y unitarias, 48 pasan. Las 3 que fallan ya fallaban antes (se comprobó contra el commit anterior): dos esperan un formato de respuesta antiguo (`/evidencia` paginado y el campo `fuente` del SLA) y una recibe 401 en el login. Corresponden a la Fase B.

## 9.3 ter. Avance de la Fase B (13 de noviembre de 2026)

| Paso | Estado |
|---|---|
| 9. Parámetro `orden` en los listados | Hecho en incidentes, activos, organizaciones y usuarios, con lista blanca de columnas y 400 si la columna no existe. Falta el encabezado ordenable en la interfaz (Fase D) |
| 10. Pruebas de integración de categoría, transiciones y auditoría | Hecho en la Fase A; ahora también de orden e IA |
| 10. Prueba de integración de IA | Hecho: endpoints con el servicio simulado, validación, roles, errores 502 y 504 en formato estándar, y el adaptador contra un servidor HTTP simulado |
| 11. OpenAPI implementado | Regenerado desde la API en ejecución (35 rutas) |

**Hallazgos corregidos**
- La refactor a servicios (`844061e`) había cambiado el formato de la bitácora, las evidencias y el cumplimiento del SLA, y el frontend espera el formato anterior (`{ total, items }` y `horasSLA`, `cumple`, `fuente`, `fechaLimite`, `cerrado`). Se restauró y se comprobó contra la API en ejecución. Con esto pasaron las 3 pruebas que fallaban.
- La clave de IA viajaba en la URL (`?key=`), por lo que podía quedar en los logs del cliente HTTP. Ahora va en el encabezado `x-goog-api-key`, con prueba.
- Las categorías del asistente simulado no coincidían con el catálogo (`Malware / Amenaza Crítica`). Ahora usan los nombres de `tb_CategoriaIncidente` y el prompt real le pide uno del catálogo, así la interfaz puede completar la categoría.
- Dos errores de las pruebas: una contraseña que no era la del seed y un servicio mal resuelto.

**Pruebas:** 73 de 73 pasan (integración y unitarias).

**Pendiente en Fase B:** si el proveedor de IA responde con un error HTTP, el adaptador usa el simulado sin avisar; solo el tiempo agotado devuelve 504. Decidir si ese comportamiento se mantiene (está en ADR-007) o si se devuelve 502.

## 9.4 Plan para cerrar los Hitos 2 y 3

Orden sugerido; cada paso termina con pruebas en verde y la documentación actualizada.

### Fase A. Base de datos (responsable: Bastián; Rafael apoya con la matriz de transiciones)
1. **Migración de estructura:** `tb_CategoriaIncidente` (+ seed: Phishing, Malware, Acceso no autorizado, Denegación de servicio, Fuga de datos, Otro), `tb_Incidente.categoria_id` (FK), `tb_TransicionEstado` (único en origen y destino, CHECK `origen <> destino`) con los pares de la matriz ADR-008.
2. **Auditoría en tablas principales:** `fecha_creacion`, `fecha_modificacion`, `creado_por_id`, `modificado_por_id` en `tb_Incidente` y `tb_Activo`; poblarlas desde el `DbContext` (`SaveChanges`) y no a mano en cada endpoint. `tb_LogAuditoria` ya tiene `ip_origen` y `timestamp`; evaluar agregar entidad, id del registro y valores anterior/nuevo (RF-16).
3. **CHECK y coherencia:** restricción que asegure que el `organizacion_id` del incidente coincide con el del activo (`05_modelo_datos.md` línea 151).
4. **Vistas para reportes:** `vw_IncidenteDetalle`, `vw_IndicadoresMensuales`, `vw_CumplimientoSla`, creadas con `migrationBuilder.Sql` y mapeadas en EF como entidades sin clave.
5. **Servicio de transiciones:** `IncidenteService` y el frontend leen la matriz desde `tb_TransicionEstado` (endpoint `GET /api/v1/estados/transiciones`) y se elimina el `Dictionary` duplicado.
6. **Categoría en la API y la interfaz:** campo en crear y editar incidente, filtro y orden; el generador sintético la rellena; la sugerencia de IA se mapea a una categoría real.
7. **Respaldo y restauración:** script `scripts/respaldo.ps1` (o `.sh`) que ejecuta `BACKUP DATABASE ... TO DISK` dentro del contenedor, genera `SentryGuard.bak`, lo restaura en una base de prueba y compara conteos. Guardar el log como `docs/evidencias/respaldo-restauracion.txt`. Decidir si el `.bak` se sube al repo (pesa) o se adjunta aparte como evidencia.
8. **Actualizar documentación:** `05_modelo_datos.md` (ER y diccionario regenerados) y quitar el estado "planificado".

### Fase B. Backend (responsable: Bastián)
9. Parámetro `orden` en los listados (RF-06) si falta, con pruebas.
10. Pruebas de integración para categoría, transiciones y auditoría; prueba de integración de `/api/v1/ia/*` con el asistente simulado.
11. Regenerar `openapi-implementado.json` y revisar que coincida con el código.

### Fase C. Contenedores, CI y seguridad (responsable: Rafael)
12. **Decidir la versión de .NET** (recomendado: .NET 10, como en la documentación, o documentar .NET 9) y alinear csproj, Dockerfile, CI y documentos; fijar versiones de imágenes.
13. **Quitar secretos por defecto** de `compose.yaml` y `.env.example`; rotar si alguna clave real llegó al historial.
14. **Arranque con un solo comando:** la API aplica migraciones al iniciar (o un servicio `migrate`), `seed` espera a la API, `web` hace proxy a `/api`. Probar `docker compose up` desde cero y con volúmenes limpios; agregar health check a `web`, `read_only`/no root donde se pueda y redes explícitas.
15. **CI completo:** jobs de backend (con SQL Server como servicio para las pruebas de integración), frontend (lint, pruebas, build), y un job `publish` que construye las imágenes y las publica en GHCR **solo si pasan los anteriores** y solo en `main`. Verificar que no se imprimen secretos.
16. Actualizar el paquete vulnerable y confirmar que `dotnet list package --vulnerable` sale limpio.

### Fase D. Frontend e IA (responsable: Adriel)
17. Verificar la interfaz completa contra la API en Docker (login por rol, CRUD, transiciones, IA con y sin clave).
18. Pantalla o selector de categoría y vista de resumen con IA (`/resumir`) si se quiere mostrar en el Hito 3.
19. Repasar a11y y responsive en las pantallas nuevas (axe y 360 px).

### Fase E. Documentación y cierre de Hito 1 (todos)
20. Registrar la aprobación del dominio, confirmar el tercer integrante y las fechas.
21. README con `docker compose up` como camino principal, tabla de pruebas actualizada y declaración de IA.
22. `AI_LOG.md`: dejar claro qué revisó cada integrante.

**Criterio de "listo para el Hito 3":** `git clone` + `cp .env.example .env` + `docker compose up` deja la aplicación usable con 500+ incidentes; CI verde con publicación condicionada; `SentryGuard.bak` restaurado con evidencia; migraciones, vistas y tablas auxiliares presentes.

## 9.5 Mirada anticipada: lo que aún falta para el 100% de la rúbrica

| Criterio de la rúbrica | Pts | Brecha principal |
|---|---|---|
| 4. Front end, responsive y accesibilidad | 15 | Prueba con lector de pantalla documentada (NVDA), capturas a 360 y 1440 px en todas las pantallas nuevas, WCAG en dashboard y reportes |
| 1. Arquitectura | 12 | Diagramas y ADR al día con el código; pruebas unitarias de la capa de negocio (ya hay una base) |
| 2. Modelo de datos | 10 | Fase A completa, índices justificados |
| 3. Backend y API | 10 | Auditoría completa de cambios y OpenAPI actualizado |
| 8. Contenedores, CI/CD y despliegue | 10 | Fase C y **plan de despliegue con los 10 apartados** (no existe aún) |
| 5. Indicadores | 8 | 6 indicadores con fórmula, pregunta de negocio e interpretación (no existe) |
| 6. Dashboard | 8 | 4 visualizaciones con filtros, endpoints de agregación y tabla de datos accesible (no existe) |
| 7. Reportes | 8 | 3 reportes y exportación a Excel, PDF y CSV, impresión y registro de quién exporta (no existe) |
| 10. Articulación e informe | 8 | Informe PDF de 11 secciones, manual de usuario por rol (no existen) |
| 9. IA | 5 | Documentación del endpoint y prueba de integración |
| 11. Defensa | 6 | Ensayo cronometrado; que cada integrante conozca todas las capas |

Dashboard, reportes e indicadores (24 puntos) dependen de las vistas de la Fase A, por eso esa fase va primero.

## 9.6 Reparto sugerido

| Integrante | Fases |
|---|---|
| Bastián Ovalle | A y B (base de datos y backend). Hoy tiene 2 commits: debe hacer los suyos aquí |
| Rafael (Rafael-Ano) | C (contenedores, CI, secretos) y la matriz de transiciones |
| Adriel Troncoso | D y E (frontend, IA en la interfaz, documentación) |
