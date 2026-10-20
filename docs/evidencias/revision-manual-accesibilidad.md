# Revisión manual de accesibilidad y diseño responsivo

Aplicación: SentryGuard (frontend React) · Norma de referencia: WCAG 2.2 nivel AA
Navegador: Chromium (Playwright 1.64 y panel de navegador de Claude Code) · Fecha: 2026-10-20

## 1. Verificación automática

| Herramienta | Alcance | Resultado | Evidencia |
|---|---|---|---|
| axe-core 4.x vía `@axe-core/playwright` (etiquetas `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa`) | Login, listado, detalle, diálogo de eliminación, formulario con errores | **0 violaciones** en las 5 vistas | `axe-reporte.json`, `pruebas-e2e.txt` |
| oxlint con el plugin `jsx-a11y` (reglas portadas de eslint-plugin-jsx-a11y) | Todo `src/` | 0 errores, 0 advertencias | `npm run lint` |
| Prueba de reflujo (Playwright) | Las mismas vistas a 320, 390, 768 y 1280 px | Sin scroll horizontal en ningún ancho | `capturas/*.png`, `pruebas-e2e.txt` |

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

## 3. Diseño responsivo (realizado)

| Verificación | Resultado |
|---|---|
| 320 px: listado en tarjetas, cabecera y filtros apilados, sin scroll horizontal | Cumple (`capturas/listado-movil-320.png`) |
| 768 px: tarjetas en 2 columnas, filtros en una fila | Cumple (`capturas/listado-tableta-768.png`) |
| 1280 px: tabla semántica con encabezados de columna y de fila | Cumple (`capturas/listado-escritorio-1280.png`) |
| Objetivos táctiles de al menos 44 × 44 px | Cumple (medido en el navegador a 320 px: ningún control interactivo menor a 44 px) |
| Zoom al 200 %: a 1280 px de ventana el ancho efectivo es 640 px y la vista pasa a tarjetas sin pérdida de contenido | Cumple (mismo diseño que el ancho de 640 px) |

## 4. Lector de pantalla (NVDA) — PENDIENTE de ejecutar por el estudiante

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
