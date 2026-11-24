# Evaluación 3 · documentación del proyecto (Hitos 1 a 4)

**Proyecto:** SentryGuard, sistema web de gestión de incidentes de ciberseguridad para una empresa ficticia (CyberOps Chile SpA), con datos sintéticos.

**Asignatura:** Lenguaje de Programación Web II · **Equipo:** Adriel Troncoso, Bastián Ovalle y Rafael Navarrete.

La pauta pide para el Hito 1: dominio aprobado, requerimientos funcionales y no funcionales, historias de usuario priorizadas, arquitectura, modelo entidad–relación, contrato de la API, maquetas de pantallas, plan de trabajo y repositorio inicializado. Cada documento distingue lo **implementado** (✅) de lo **planificado** (📋, con su hito), para que la documentación coincida con el código en cada entrega.

| # | Documento | Contenido |
|---|---|---|
| 1 | [01_propuesta_dominio.md](01_propuesta_dominio.md) | Contexto, problema, objetivo, roles, 13 casos de uso, flujo de estados, alcance del producto mínimo viable |
| 2 | [02_requerimientos_historias.md](02_requerimientos_historias.md) | 25 requerimientos funcionales y 12 no funcionales priorizados, 13 historias de usuario con criterios de aceptación, matriz de permisos |
| 3 | [03_arquitectura.md](03_arquitectura.md) | Capas, diagramas de componentes y de despliegue, patrones, SOLID, cómo se atienden los RNF |
| 4 | [04_decisiones_adr.md](04_decisiones_adr.md) | 11 decisiones de arquitectura con sus alternativas descartadas |
| 5 | [05_modelo_datos.md](05_modelo_datos.md) | Diagrama ER y diccionario de las 15 tablas (generados desde el esquema real), normalización, índices, cambios del Hito 2, migraciones, respaldo |
| 6 | [06_contrato_api.md](06_contrato_api.md) | Recursos, códigos de estado, modelo de errores, seguridad; [OpenAPI implementado](contrato-api/openapi-implementado.json) y [OpenAPI de los endpoints nuevos](contrato-api/openapi-evaluacion3.yaml) |
| 7 | [07_maquetas_guia_estilos.md](07_maquetas_guia_estilos.md) | Paleta con contrastes, tipografía, espaciado, componentes y estados; maquetas por rol (pantallas actuales y nuevas) |
| 8 | [08_plan_trabajo.md](08_plan_trabajo.md) | Equipo y responsabilidades, hitos, herramientas, organización del repositorio, definición de terminado, entorno de contenedores previsto, riesgos |
| 11 | [11_plan_hito4_sonnet.md](11_plan_hito4_sonnet.md) | Plan de ejecución del Hito 4 con prompts por tarea (herramienta de trabajo del equipo) |
| 12 | [12_plan_despliegue.md](12_plan_despliegue.md) | Plan de despliegue de 10 apartados, con la reversión probada ([evidencia](evidencias/reversion-migracion.md)) |
| 13 | [13_informe_integracion.md](13_informe_integracion.md) | Qué componente evidencia cada asignatura y trazabilidad requerimiento → componente → asignatura |
| 14 | [14_manual_usuario.md](14_manual_usuario.md) | Manual de usuario breve por rol |
| 15 | [15_pendientes_finales.md](15_pendientes_finales.md) | Estado de los pendientes finales, con paso a paso y prompts de IA |
| — | [INFORME_TECNICO_EV3.md](INFORME_TECNICO_EV3.md) | Informe técnico con las 11 secciones de la pauta (exportar a PDF para la entrega) |
| — | [ejemplos/](ejemplos/) | Archivos de ejemplo exportados (3 reportes × Excel, PDF y CSV) y las respuestas JSON del dashboard |
| — | [INTEGRACION_SENTRYGUARD2.md](INTEGRACION_SENTRYGUARD2.md) | Qué se trae del fork de Bastián, en qué orden y cómo (Hito 2) |

**Diagramas:** los archivos fuente (`.mmd`, Mermaid) y sus exportaciones (`.svg` y `.png`) están en [diagramas/](diagramas/). GitHub muestra los bloques Mermaid directamente en los documentos.

**Maquetas:** las capturas y las imágenes de las maquetas están en [maquetas/](maquetas/), y el HTML de las maquetas nuevas, en [maquetas/fuente/](maquetas/fuente/).

## Pendientes antes de entregar

- [ ] Registrar la aprobación del dominio por el docente en `01_propuesta_dominio.md`.
- [ ] Confirmar el tercer integrante y completar `08_plan_trabajo.md` §8.1.
- [ ] Confirmar las fechas de cada semana con el docente (`08_plan_trabajo.md` §8.2).
