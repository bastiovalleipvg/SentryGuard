# 6. Contrato de la API

| Archivo | Qué contiene |
|---|---|
| [contrato-api/openapi-implementado.json](contrato-api/openapi-implementado.json) | El contrato **de lo ya implementado**, exportado desde la API en ejecución (`GET /openapi/v1.json`): 30 rutas y 52 operaciones |
| [contrato-api/openapi-evaluacion3.yaml](contrato-api/openapi-evaluacion3.yaml) | El contrato **de los endpoints nuevos**, escrito en el Hito 1 **antes de implementarlos**. Validado con Redocly CLI |

Con la API corriendo en desarrollo, la documentación interactiva está en `https://localhost:7166/scalar/v1`. Tiene un panel para pegar el token Bearer.

## 6.1 Recursos

Todas las rutas van bajo `/api/v1/`.

| Recurso | Rutas principales | Estado |
|---|---|---|
| Usuarios y sesión | `POST /usuarios/login`, `GET /usuarios/me`, `GET`, `POST /usuarios/registro`, `PUT /usuarios/{id}`, `GET /usuarios/analistas` | ✅ |
| Incidentes | `GET`/`POST /incidentes`, `GET`/`PUT`/`DELETE /incidentes/{id}`, `POST /incidentes/{id}/asignar`, `POST /incidentes/{id}/cambiar-estado`, `GET`/`POST /incidentes/{id}/bitacora`, `GET`/`POST /incidentes/{id}/evidencia`, `GET /incidentes/sla/cumplimiento/{id}` | ✅ |
| Incidentes (cambios) | `orden`, `categoriaId`, `desde` y `hasta` en el listado; `GET /incidentes/{id}/transiciones`; 409 ante una transición inválida | 📋 Hito 2 |
| Activos | `GET`/`POST /activos`, `GET`/`PUT`/`DELETE /activos/{id}` | ✅ |
| Organizaciones y contratos SLA | `GET`/`POST /organizaciones`, `GET`/`PUT`/`DELETE /organizaciones/{id}`, `GET`/`POST /organizaciones/{id}/contratos-sla`, `POST …/{contratoId}/finalizar`, `DELETE …/{contratoId}` | ✅ |
| Catálogos | `severidades`, `estados-incidente`, `categorias-activo`, `planes-servicio`, `roles` | ✅ |
| Catálogos (nuevo) | `categorias-incidente` | 📋 Hito 2 |
| Auditoría | `GET /auditoria` | ✅ |
| Dashboard | `GET /dashboard/indicadores`, `GET /dashboard/series` | 📋 Hito 4 |
| Reportes | `GET /reportes/{operativo\|gerencial\|sla}`, `GET /reportes/{tipo}/exportar?formato=xlsx\|pdf\|csv` | 📋 Hito 4 |
| IA | `POST /ia/clasificar-incidente`, `POST /ia/resumir` | 📋 Hito 3 |
| Salud | `GET /health` | 📋 Hito 3 |

## 6.2 Verbos y códigos de estado

| Código | Cuándo |
|---|---|
| 200 | Lectura o actualización correcta |
| 201 | Creado, con la cabecera `Location` |
| 204 | Eliminado |
| 400 | Datos inválidos, con los errores por campo |
| 401 | Sin token, o con un token inválido o expirado |
| 403 | El rol o la organización no permiten la operación (OWASP API1 y API5) |
| 404 | No existe |
| 409 | Conflicto de negocio: borrar algo que tiene datos asociados o hacer una transición de estado inválida |
| 429 | Límite de tasa superado, con la cabecera `Retry-After` |
| 502 / 504 | El servicio de IA falló o no respondió a tiempo |
| 500 | Error inesperado; responde un mensaje genérico, sin detalles internos |

## 6.3 Modelo de errores

**Hoy conviven tres formatos:**
- `{ codigo, mensaje, traceId }`, del middleware global.
- `ValidationProblem` de ASP.NET para los 400.
- Algunos `BadRequest(new { error })`.

**Desde el Hito 2, toda la API responde con un solo formato**, *ProblemDetails* (RFC 9457), con dos extensiones:

```json
{
  "type": "https://sentryguard.local/errores/transicion-invalida",
  "title": "Transición de estado no permitida",
  "status": 409,
  "detail": "No se puede pasar de Abierto a Cerrado.",
  "codigo": "TRANSICION_INVALIDA",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": { "estadoId": ["Transición no permitida."] }
}
```

- `codigo` es estable y lo usa el frontend.
- `detail` está escrito para el usuario final.
- `errors` aparece solo en los 400 de validación.
- La base es `Common/ApiResults.cs` de SentryGuard-2 (ver [INTEGRACION_SENTRYGUARD2.md](INTEGRACION_SENTRYGUARD2.md)).

## 6.4 Seguridad

- **Esquema `Bearer` (JWT HS256):** el token se obtiene en `POST /usuarios/login` y dura 60 minutos. Los únicos endpoints sin token son el login, los `GET` de catálogos y `/health`.
- **Autorización por rol** en cada endpoint (`RequireRole`) y **por objeto** dentro de cada uno (`AuthService.ValidaAccesoIncidente` y `ValidaAccesoOrganizacion`). La matriz está en [02_requerimientos_historias.md](02_requerimientos_historias.md#24-matriz-de-permisos).
- **Límites de tasa:**
  - Login: 5 por minuto por IP.
  - Creación de incidentes: 10 por minuto por IP.
  - *Previstos:* IA, 10 por minuto por usuario; exportaciones, 20 por minuto por usuario.
- **Paginación acotada** a 50 elementos por página en todos los listados.
