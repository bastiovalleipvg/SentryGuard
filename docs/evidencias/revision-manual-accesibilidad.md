# Revisión manual de accesibilidad y diseño responsivo

Aplicación: SentryGuard (frontend React) · Norma de referencia: WCAG 2.2 nivel AA
Navegador: Chromium (Playwright 1.64 y panel de navegador de Claude Code) · Fecha: 2026-10-20, ampliada el 2026-10-25 con las vistas de los recursos relacionados

## 1. Verificación automática

| Herramienta | Alcance | Resultado | Evidencia |
|---|---|---|---|
| axe-core 4.x vía `@axe-core/playwright` (etiquetas `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa`) | Login, listado, detalle (con SLA, bitácora y evidencias), diálogo de eliminación, formulario con errores, activos (listado, detalle, formulario), organizaciones (listado y detalle con contratos SLA), usuarios (listado y formulario), catálogos, auditoría y menú móvil desplegado | **0 violaciones** en las 18 vistas (15 de los Trabajos 1 y 2 + 3 del Hito 4) | `axe-reporte.json`, `pruebas-e2e.txt` |
| oxlint con el plugin `jsx-a11y` (reglas portadas de eslint-plugin-jsx-a11y) | Todo `src/` | 0 errores, 0 advertencias | `npm run lint` |
| Prueba de reflujo (Playwright) | Listado, detalle, formulario con errores, activos, detalle de organización, usuarios y catálogos a 320, 390, 768 y 1280 px | Sin scroll horizontal en ningún ancho | `capturas/*.png`, `pruebas-e2e.txt` |

## 2. Revisión solo con teclado (realizada)

Recorrido hecho con Tab, Shift+Tab, Enter y Escape, sin mouse.

| Verificación | Resultado |
|---|---|
| El primer Tab tras cargar una página llega a «Saltar al contenido principal», que se hace visible al recibir el foco | Cumple (automatizado en `accesibilidad.spec.ts`) |
| Enter sobre ese enlace lleva el foco a `<main>` y el siguiente Tab al primer control del contenido | Cumple (automatizado) |
| Todos los controles muestran un contorno de foco visible (3 px, contraste alto; amarillo sobre la cabecera oscura) | Cumple (automatizado: `outline-style` distinto de `none`) |
| Al cambiar de ruta, el foco pasa al `<h1>` de la nueva vista y el título de la pestaña cambia | Cumple (verificado en el navegador tras crear un incidente) |
| Formulario inválido: el foco va al primer campo con error y el resumen se anuncia (`role="alert"`) | Cumple (verificado en el navegador y en `incidentes.test.tsx`) |
| Diálogo «¿Eliminar este incidente?»: al abrir, el foco queda en «Cancelar»; Escape lo cierra; el foco vuelve a «Eliminar» | Cumple (verificado en el navegador y automatizado) |
| No hay trampas de foco fuera del diálogo modal | Cumple |
| Orden de tabulación sigue el orden visual en móvil y escritorio | Cumple |
| Menú móvil: el botón «Menú» informa `aria-expanded`, despliega los enlaces del rol y se cierra al navegar | Cumple (verificado en el navegador a 320 px y en `mantenedores.test.tsx`) |
| Catálogos: «Editar» lleva el foco al encabezado del formulario («Editar “Servidor”») con los datos cargados | Cumple (verificado en el navegador) |
| Botones repetidos por fila («Editar», «Eliminar», «Finalizar») tienen nombre accesible único con el elemento («Editar Servidor») | Cumple |
| Diálogos de eliminar activo, organización, contrato o elemento de catálogo se comportan igual que el de incidentes | Cumple (mismo componente `ConfirmDialog`) |

## 3. Diseño responsivo (realizado)

| Verificación | Resultado |
|---|---|
| 320 px: listado en tarjetas, cabecera y filtros apilados, sin scroll horizontal | Cumple (`capturas/listado-movil-320.png`) |
| 768 px: tarjetas en 2 columnas, filtros en una fila | Cumple (`capturas/listado-tableta-768.png`) |
| 1280 px: tabla semántica con encabezados de columna y de fila | Cumple (`capturas/listado-escritorio-1280.png`) |
| Listados de activos, organizaciones, usuarios, catálogos y auditoría: tarjetas en móvil, tabla desde 768 px (usuarios desde 1024 px) | Cumple (`capturas/activos-*.png`, `usuarios-*.png`, `catalogos-*.png`) |
| Menú principal plegado bajo 1024 px | Cumple (`capturas/menu-movil-320.png`) |
| Objetivos táctiles de al menos 44 × 44 px | Cumple (medido en el navegador a 320 px: ningún control interactivo menor a 44 px) |
| Zoom al 200 %: a 1280 px de ventana el ancho efectivo es 640 px y la vista pasa a tarjetas sin pérdida de contenido | Cumple (mismo diseño que el ancho de 640 px) |

## 4. Lector de pantalla (NVDA): pendiente, la ejecuta el estudiante

Esta revisión no se puede automatizar: requiere usar NVDA en Windows y escuchar lo que anuncia. Pasos sugeridos (NVDA + Chrome o Firefox, modo exploración con H para encabezados y D para regiones):

| Paso | Qué debería anunciarse | Resultado |
|---|---|---|
| Abrir `/login` | «Iniciar sesión, encabezado nivel 1»; campos «Correo electrónico (obligatorio)», «Contraseña (obligatorio)» | ☐ |
| Enviar el login vacío | Los mensajes de error de cada campo al recibir el foco | ☐ |
| Credenciales incorrectas | «Correo o contraseña incorrectos…» anunciado automáticamente (alerta) | ☐ |
| Tras iniciar sesión | «Incidentes, encabezado nivel 1»; regiones «Principal» (navegación), principal, «Filtrar incidentes» (búsqueda) | ☐ |
| Aplicar un filtro | «N incidentes encontrados» anunciado (región de estado) | ☐ |
| Crear un incidente | «Incidente registrado correctamente» anunciado | ☐ |
| Abrir «Eliminar» | «¿Eliminar este incidente?, diálogo» y el texto de advertencia | ☐ |
| Insignias de severidad/estado | «Severidad: Alta», «Estado: Abierto» (texto oculto visualmente) | ☐ |

Observaciones del estudiante:

- …

## 6. Hito 4: dashboard y reportes (23 de noviembre de 2026)

| Verificación | Resultado |
|---|---|
| axe (WCAG 2.2 AA) en `/dashboard`, `/reportes` y `/reportes?tipo=gerencial` | **0 violaciones**; en total, 18 vistas auditadas (`axe-reporte.json`) |
| Reflujo a 320, 360, 390, 768, 1280 y 1440 px | Sin scroll horizontal en el dashboard ni en reportes |
| Gráficos | Cada gráfico lleva título, la conclusión en texto, `role="img"` con nombre accesible y una tabla con los mismos datos (`<details>`). El significado no depende del color: los valores se escriben sobre cada barra o punto |
| Variación de los indicadores | Se escribe con texto y signo («▲ +12 respecto del período anterior»), no solo con color |
| Resumen con IA | La región `aria-live="polite"` anuncia el resultado y la etiqueta «Generado con IA» |
| Tabla ancha del reporte operativo | El contenedor con desplazamiento es una región con nombre y `tabIndex=0`, así que se puede recorrer con el teclado (lo detectó axe y se corrigió) |
| Impresión | `@media print` oculta menú, filtros y botones, y muestra título, fecha de emisión y filtros usados |

### Pendiente a mano: recorrido con NVDA

Esta parte **no se puede automatizar**. Completar con NVDA y Firefox o Chrome, solo con el teclado, y anotar lo que se anuncia:

| Paso | Qué se espera que anuncie | Lo que anunció | ¿Cumple? |
|---|---|---|---|
| 1. Iniciar sesión (error de credenciales y éxito) | El mensaje de error; luego el título de la vista | | |
| 2. Crear un incidente y usar «Sugerir con IA» | La sugerencia aparece anunciada y no se aplica sola | | |
| 3. Abrir el dashboard | Título «Dashboard»; cada indicador con su valor y su fórmula | | |
| 4. Recorrer un gráfico y abrir «Ver los datos en tabla» | Conclusión del gráfico; tabla con encabezados de fila | | |
| 5. Pulsar «Resumir con IA» | «Generado con IA» y el texto del resumen | | |
| 6. Abrir un reporte, filtrarlo y exportarlo | Cambio de reporte (`aria-current`), cantidad de filas y el aviso «Archivo generado» | | |

## 7. Simulación automatizada de lector de pantalla y teclado (23 de noviembre de 2026)

**Esto NO reemplaza el recorrido con NVDA de la sección 6**: es una comprobación hecha con Playwright contra el sistema en Docker (`http://localhost:8088`) que inspecciona lo que un lector de pantalla recibe (nombres, roles, estados, regiones `aria-live`, foco) y recorre el flujo solo con teclado. Nadie escuchó la voz de NVDA; la sección 6 sigue pendiente.

| Verificación | Resultado |
|---|---|
| Login solo con teclado: `Tab` llega a «Correo electrónico (obligatorio)», luego «Contraseña (obligatorio)»; `Enter` envía | Cumple |
| Al entrar y al navegar, el foco queda en el `<h1>` de la vista y el título de la pestaña cambia («Dashboard · SentryGuard») | Cumple |
| Encabezados del dashboard (lo que recorre la tecla `H`): Dashboard, Indicadores, los 6 gráficos y «Resumen del período» | Cumple, en orden lógico |
| 6 indicadores con valor, variación en texto y fórmula | Cumple |
| 6 gráficos: todos con nombre accesible (`aria-label`), conclusión escrita y tabla con los datos | Cumple (0 sin nombre, 0 sin tabla) |
| Filtros de fecha con teclado | El campo de fecha de Chrome tiene 3 paradas (día, mes, año) por cada fecha: es el comportamiento nativo del navegador |
| «Resumir con IA» con teclado (`Enter`) | El resultado aparece dentro de una región `role="status"` (`aria-live="polite"`) con «Generado con IA» |
| Reportes: botón «Gerencial» con `Enter` | `aria-current="page"` cambia al nuevo reporte; la cantidad de filas («52 filas.») está en una región `status` |
| Tabla ancha | Región con nombre («Reporte gerencial») y `tabindex=0`: se recorre con el teclado |
| Exportar CSV con teclado | Descarga `reporte-gerencial-2026-11-23.csv` y anuncia «Archivo generado» |
| Formulario inválido | `role="alert"`: «Revisa los 4 campos marcados antes de continuar.»; el foco pasa al primer campo con error, que tiene `aria-invalid="true"` y `aria-describedby` hacia su mensaje |

**Hallazgos:** ninguno que corregir. Queda para NVDA verificar cómo se *pronuncian* estos textos (por ejemplo, los símbolos «▲» y «✨»).
