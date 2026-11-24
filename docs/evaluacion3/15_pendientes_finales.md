# 15. Pendientes finales: paso a paso y prompts de IA

Todo el código y la documentación están listos y `main` está en verde. Lo que queda requiere a una persona: probar en una máquina real, escribir de tu propia mano y presentar. Cada punto trae el paso a paso, cuánto tarda y, cuando sirve, un prompt para pegar en una IA.

## Estado al 23 de noviembre de 2026

| # | Pendiente | Estado |
|---|---|---|
| 1 | Probar con Docker, reversión de imagen y PR #18 | **Hecho**: `evidencias/reversion-imagen.md`; el PR #18 (nginx 1.31.7) se probó y se fusionó. Falta solo que lo repitas tú en tu defensa |
| 2 | Recorrido con NVDA | **Pendiente (solo una persona)** |
| 3 | Aprobación del docente | **Pendiente (solo una persona)** |
| 4 | Informe técnico en PDF | **Hecho**: `INFORME_TECNICO_EV3.pdf` (con el aporte de cada integrante). Falta tu nombre en la portada si el docente lo pide, y volver a exportar si cambias el texto |
| 5 | `AI_LOG.md` | **Hecho** para este hito (§3.16); cada integrante agrega lo suyo |
| 6 | Verificar licencia y costos | **Hecho** (con fuente y fecha, en el apartado 9 del plan de despliegue) |
| 7 | Presentación | **Borrador hecho**: `docs/Presentacion_Evaluacion3.pptx` (14 diapositivas con notas del orador). Revísala en PowerPoint (no pude abrirla para ver el diseño) y ensaya los 20 minutos |
| 8 | Preguntas individuales | **Pendiente (solo una persona)** |
| 9 | Cierre del repositorio | Parcial: ver el paso 9 |

Los pasos de abajo se conservan por si hay que repetirlos o mostrarlos.

**Orden recomendado:** 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9. Tiempo total estimado: 5 a 6 horas.

**Regla para los prompts:** pega siempre el dato real (cifras, textos) y revisa lo que devuelve la IA. Todo lo entregado debe poder explicarse en vivo, y lo que uses de una IA se declara en `AI_LOG.md`.

---

## 1. Probar el sistema completo con Docker en tu máquina (30 min)

El CI ya levanta todo con `docker compose`, pero la defensa es en vivo en un equipo real.

1. Abre **Docker Desktop** y espera a que diga «Engine running».
2. En PowerShell, en la raíz del repositorio:
   ```powershell
   git pull
   ./scripts/generar-env.ps1
   docker compose up --build
   ```
3. Espera 3 a 5 minutos. Debe terminar el contenedor `seed` con código 0 (`sentryguard-seed exited (0)`) y `db`, `api` y `web` quedar *healthy* (`docker compose ps`).
4. Abre `http://localhost:8088` y comprueba:
   - [ ] Ingresas con `admin@sentryguard.cl` / `Admin123!`.
   - [ ] El **Dashboard** muestra 6 indicadores y 6 gráficos.
   - [ ] En **Reportes** exportas Excel, PDF y CSV, y los abres.
   - [ ] **Imprimir** muestra una vista sin menú ni botones.
   - [ ] `http://localhost:8088/health` responde.
5. Prueba cada rol (`supervisor@`, `analista@`, `cliente@sentryguard.cl`) y confirma que cada uno ve solo lo suyo (tabla de permisos en `14_manual_usuario.md`).
6. **Respaldo** (opcional, ya hay evidencia): `$env:SA_PASSWORD = '<la de .env>'; ./scripts/respaldo-restauracion.ps1`.
7. **Prueba de reversión de imagen** (apartado 5 del plan de despliegue):
   ```powershell
   git log --oneline -3                 # anota un commit anterior
   git checkout <commit-anterior>
   docker compose up -d --build         # versión anterior
   docker compose ps; curl http://localhost:8088/health
   git checkout main
   docker compose up -d --build         # vuelve a la actual
   ```
   Guarda los comandos y la salida en `docs/evaluacion3/evidencias/reversion-imagen.md`.
8. Para empezar de cero: `docker compose down -v`.

**Si algo falla:** `docker compose logs api --tail 80` y pega el error en la IA (prompt A, al final).

### PR #18 de Dependabot (nginx 1.28 → 1.31)
1. Con el sistema ya levantado, abre la rama del PR: `gh pr checkout 18`.
2. Repite el paso 2 y la lista del paso 4.
3. Si todo funciona: `gh pr merge 18 --squash`. Si no: `gh pr close 18 -c "Se cierra: no se prueba antes de la entrega."`
4. Vuelve a `main`: `git checkout main`.

---

## 2. Recorrido con NVDA (45 min)

No se puede automatizar y la pauta lo pide.

**Preparación**
1. Descarga NVDA desde `https://www.nvaccess.org` (gratis) e instálalo.
2. Abre la aplicación (paso 1) en Firefox o Chrome.
3. Inicia NVDA. Teclas útiles: `Insert` (tecla NVDA), `Tab` y `Shift+Tab` para moverte, `H` para saltar de encabezado en encabezado, `T` para tablas, `Insert+F7` para la lista de elementos, `Insert+Q` para salir.
4. Cierra los ojos o apaga el monitor al menos en una parte, para probar de verdad.

**Qué hacer**
1. Abre `docs/evidencias/revision-manual-accesibilidad.md` y ve a la sección 6, «Pendiente a mano».
2. Completa las 6 filas: en «Lo que anunció» escribe con tus palabras lo que dijo NVDA, y marca «¿Cumple?» con Sí o No.
3. Si algo no cumple (por ejemplo, un botón sin nombre), anótalo en una sección «Hallazgos» al final, con la vista y lo que dijo NVDA. Si es corto de arreglar, abre una incidencia o pídele a la IA el arreglo (prompt B).
4. Agrega arriba la fecha, la versión de NVDA y el navegador.

**Commit:** `docs(a11y): recorrido con NVDA del flujo principal y del dashboard`

---

## 3. Aprobación del docente del Hito 1 (10 min)

1. Si tienes un correo, mensaje o nota del docente que aprueba el dominio, guarda una captura en `docs/evaluacion3/evidencias/aprobacion-docente.png`.
2. En `docs/evaluacion3/01_propuesta_dominio.md` reemplaza la línea `**Aprobación del docente:** [PENDIENTE: ...]` por la fecha y la forma (por ejemplo, «Aprobado por correo el 6 de noviembre de 2026; ver `evidencias/aprobacion-docente.png`»).
3. Si no hay aprobación todavía, pídela hoy por correo. Plantilla:
   > Estimado/a profesor/a: le escribo en nombre del equipo (Adriel Troncoso, Bastián Ovalle y Rafael Navarrete) para confirmar la aprobación del dominio de la Evaluación 3: un sistema de gestión de incidentes de ciberseguridad para una empresa ficticia (CyberOps Chile SpA) con datos sintéticos. La propuesta está en `docs/evaluacion3/01_propuesta_dominio.md` del repositorio. ¿Podría confirmarnos por este medio que el dominio está aprobado? Gracias.

**Commit:** `docs: registrar la aprobacion del dominio por el docente`

---

## 4. Informe técnico en PDF con tu nombre (30 min)

1. Abre `docs/evaluacion3/INFORME_TECNICO_EV3.md`. En el encabezado, el equipo ya está; agrega la sección «Aporte de cada integrante» de la sección 11 (ver el prompt C si quieres ayuda para redactarla).
2. Revisa la sección 7: cambia «pendiente a mano» por el resultado real de NVDA.
3. **Exportar a PDF** (elige una opción):
   - *VS Code:* instala la extensión «Markdown PDF» → clic derecho en el archivo → «Markdown PDF: Export (pdf)».
   - *Pandoc* (si lo tienes): `pandoc docs/evaluacion3/INFORME_TECNICO_EV3.md -o INFORME_TECNICO_EV3.pdf --pdf-engine=xelatex`
   - *Navegador:* abre el `.md` con la vista previa de GitHub y usa «Imprimir → Guardar como PDF».
4. Abre el PDF y revisa que las tablas no se corten y que se vean los acentos.
5. Los diagramas (PNG) están en `docs/evaluacion3/diagramas/`; si quieres incluirlos en el PDF, pega las imágenes del `er-actual.png` y `componentes.png` en las secciones 4 y 5.
6. Guarda el PDF final en `docs/evaluacion3/INFORME_TECNICO_EV3.pdf` y **commitéalo** (la pauta pide el PDF).

---

## 5. `AI_LOG.md` y declaración de IA (30 min)

La pauta exige declarar herramienta y propósito, y que puedas explicar todo lo entregado.

1. Abre `AI_LOG.md` y agrega **una entrada por sesión de trabajo con IA** con este formato:
   ```
   ### <fecha> · <herramienta> · <quién>
   - **Propósito:** ...
   - **Prompt (resumen):** ...
   - **Qué aceptamos:** ...
   - **Qué corregimos o descartamos:** ...
   ```
2. **Qué registrar de este hito** (hazlo con tus palabras; lo que sigue es una guía):
   - Claude Code (Sonnet y Opus): corregir la compilación y la migración CHECK; reportes y exportación (ClosedXML y QuestPDF); dashboard y reportes en la interfaz; pruebas; plan de despliegue e informes; arreglos del CI.
   - Qué corregiste tú o la IA se equivocó: por ejemplo, la restricción CHECK duplicada entre dos migraciones, el id de la auditoría en las altas y la etiqueta de `trivy-action` que ya no existía.
3. Cada integrante registra sus propios prompts (Rafael y Bastián, los suyos).
4. Confirma que la tabla del `README.md` («Uso de IA en el desarrollo») incluye todas las herramientas que usó el equipo.

**Commit:** `docs: bitacora de uso de ia del hito 4`

---

## 6. Verificar lo marcado «NO VERIFICADO» (20 min)

1. **Licencia de QuestPDF:** abre `https://www.questpdf.com/license/` y lee las condiciones de la licencia *Community* (hoy se usa `LicenseType.Community` en `ExportadorPdf.cs`). Anota en `12_plan_despliegue.md` (apartado 9, fila QuestPDF) la condición vigente y la fecha de consulta. Si no cumples la condición para un uso comercial, ponlo como riesgo (para esta evaluación es un uso académico).
2. **Costos de nube:** elige un proveedor (DigitalOcean, Hetzner, AWS Lightsail u otro), mira el precio de una VM de 2 vCPU y 4 GB, y reemplaza los rangos de la tabla del apartado 9 por el precio real con la fuente y la fecha. Quita el `[NO VERIFICADO]` solo de lo que verificaste.
3. **Calendario:** confirma con el docente las fechas de los hitos (`08_plan_trabajo.md` §8.2) y quita la marca `[NO VERIFICADO]`.

---

## 7. Presentación de la defensa (2 a 3 horas)

El `.pptx` del Trabajo 2 está desactualizado. La pauta pide **20 minutos de exposición y 10 de preguntas**, con 7 puntos:

| Min | Punto | Qué mostrar |
|---|---|---|
| 2 | 1. Propósito y usuarios | El problema de CyberOps Chile SpA y los 4 roles |
| 5 | 2. Demostración en vivo | Un flujo por rol: Cliente crea un incidente con la IA e intenta un dato inválido; Supervisor lo asigna; Analista documenta; Administrador ve el dashboard. Repetir una pantalla en tamaño celular y **solo con teclado** |
| 3 | 3. Reportes | Un reporte con filtros, exportar a Excel, PDF y CSV y la vista de impresión |
| 3 | 4. Arquitectura y decisiones | Diagrama de componentes y 2 o 3 ADR (Gemini, flujo de estados, auditoría) |
| 2 | 5. Articulación del módulo | La tabla de `13_informe_integracion.md` |
| 3 | 6. CI/CD, IA y despliegue | El pipeline verde en GitHub, el endpoint de IA y el plan de despliegue |
| 2 | 7. Métricas y lecciones | 115 + 53 pruebas, axe sin violaciones, lección de la integración |

**Prompt D (armar las diapositivas):**
> Eres un asistente que arma presentaciones académicas. Crea el esquema de una presentación de 14 diapositivas para la defensa del proyecto «SentryGuard», un sistema de gestión de incidentes de ciberseguridad para una empresa ficticia (CyberOps Chile SpA), de 20 minutos. Sigue esta estructura de tiempos: propósito y usuarios (2 min), demostración en vivo por rol (5), reportes (3), arquitectura y decisiones (3), articulación del módulo con Orientación a Objetos II, Base de Datos, Inteligencia de Negocios I y II y Programación Web II (2), CI/CD, IA y plan de despliegue (3), métricas y lecciones (2). Para cada diapositiva dame: título, 3 viñetas cortas (máximo 12 palabras cada una) y una nota del orador de 2 o 3 frases. Datos reales que debes usar: 115 pruebas de backend y 53 de componentes en verde; axe sin violaciones en 18 vistas; 6 indicadores (volumen 468, cumplimiento del SLA 83,77 %, tiempo medio de resolución 10,05 h, backlog 49, críticos 9,62 %); reportes operativo, gerencial y de SLA exportables a Excel, PDF y CSV; pipeline con backend, frontend, contenedores, Trivy y publicación en GHCR; IA con Gemini detrás de un adaptador. No inventes cifras que no estén aquí. Escribe en español, sin jerga innecesaria.

Después, pega el esquema en PowerPoint o Google Slides. Los gráficos y capturas ya existen en `docs/evidencias/capturas/` y los diagramas en `docs/evaluacion3/diagramas/`.

**Ensayo cronometrado** (obligatorio en el Hito 4): practiquen los 20 minutos con cronómetro, **cada integrante exponiendo una parte**, y anoten cuánto tarda cada bloque.

---

## 8. Preparar las preguntas individuales (1 hora)

El criterio de la defensa es individual: cada integrante debe poder explicar **cualquier parte**, incluso las de otros. Repasen estas preguntas probables.

1. ¿Qué pasa si un Cliente cambia el ID del incidente en la URL? *(Aislamiento por organización, `AuthService.ValidaAccesoIncidente`; responde 403 o 404.)*
2. ¿Por qué el estado «Cerrado» no puede volver a «Abierto»? *(Tabla `tb_TransicionEstado`, ADR-008; la API responde 409.)*
3. ¿Dónde se calcula el cumplimiento del SLA y por qué en una vista? *(`vw_CumplimientoSla`; la lógica en un solo lugar.)*
4. ¿Qué patrones usaron y dónde? *(Strategy en `IExportador`, Adapter en `IAsistenteIA`, Repository, DTO.)*
5. ¿Qué pasa si la IA no responde? *(502 o 504 con el formato estándar; el formulario sigue funcionando; ADR-007.)*
6. ¿Cómo se protege la clave de la IA y la de JWT? *(Variables de entorno, `.env` ignorado, historial limpiado: ADR-011.)*
7. ¿Qué mitiga el rate limiting? *(Fuerza bruta en el login: 5 por minuto; exportaciones: 20 por minuto.)*
8. ¿Cómo agregarían un formato de exportación nuevo? *(Una clase que implemente `IExportador` y su registro en `Program.cs`.)*
9. ¿Qué limitaciones tiene el sistema? *(Sección 11 del informe técnico.)*
10. ¿Cómo revertirían una migración que salió mal? *(`dotnet ef database update <anterior>` o restaurar el respaldo; apartado 5 del plan de despliegue.)*

**Prompt E (simulacro de preguntas):**
> Actúa como un docente de Programación Web II que evalúa la defensa del proyecto «SentryGuard» (API .NET 10 con Minimal APIs, EF Core y SQL Server; frontend React; JWT con 4 roles; dashboard, reportes con exportación y plan de despliegue). Hazme una pregunta técnica a la vez, una por turno, sobre una parte distinta del proyecto (seguridad, base de datos, arquitectura, pruebas, accesibilidad, contenedores o IA). Espera mi respuesta, dime qué estuvo bien y qué faltó, y sigue con la siguiente. Después de 10 preguntas, dame un resumen de mis puntos débiles.

---

## 9. Cierre del repositorio (15 min)

1. `git pull` y verifica que el CI de `main` esté verde.
2. Revisa el `README.md`: arranca con `docker compose up`, tiene `.env.example` y la declaración de IA.
3. Confirma que no hay secretos: `git ls-files | grep -iE "\.env$|secret|\.pem$"` no debe devolver nada (salvo `.env.example`).
4. Comprueba que cada integrante tiene commits propios: `git shortlog -sne`.
5. Entrega el enlace del repositorio. Si es privado, da acceso al docente.

---

## Prompts de ayuda

**Prompt A (un error de Docker o del sistema):**
> Tengo este error al ejecutar `docker compose up --build` en Windows con Docker Desktop, en un proyecto con SQL Server 2022, una API .NET 10 y nginx. Este es el resultado de `docker compose ps` y de `docker compose logs api --tail 80`: «<pega aquí>». Dime la causa más probable y el comando exacto para arreglarlo, sin borrar mis datos si no es necesario.

**Prompt B (un hallazgo de accesibilidad):**
> En mi aplicación React 19 con TypeScript, NVDA anuncia «<lo que dijo>» al llegar a «<elemento>» en la pantalla «<vista>», pero debería anunciar «<lo esperado>». Este es el componente: «<pega el código>». Propón el cambio mínimo para cumplir WCAG 2.2 AA, usando HTML semántico y ARIA solo si hace falta, y explica por qué.

**Prompt C (redactar el aporte de cada integrante):**
> Redacta en español, en un párrafo por persona, el aporte de cada integrante de un equipo de tres personas para la sección «Articulación del módulo y cierre» de un informe técnico. Usa solo estos hechos: «<pega aquí lo que hizo cada uno, según `git shortlog -sne` y `git log --author`>». No exageres ni inventes tareas. Cierra con una frase sobre cómo se coordinó el equipo.
