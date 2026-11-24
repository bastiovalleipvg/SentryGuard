# Prueba de reversión de la versión desplegada (con Docker)

**Fecha:** 23 de noviembre de 2026 · **Entorno:** Docker Desktop (Engine 29.8.2) en Windows, `docker compose` con `db`, `api`, `seed` y `web`.

**Objetivo:** comprobar el apartado 5 del plan de despliegue: volver a una versión anterior del código con `docker compose up -d --build` **sin perder la base de datos**.

## Estado inicial (versión actual, `main`)

```
$ docker compose up --build -d
$ docker compose ps
sentryguard-db   (healthy)    sentryguard-api (healthy)    sentryguard-web (healthy)
sentryguard-seed exit=0 (genera 520 incidentes una sola vez)

$ curl http://localhost:8088/health            -> 200  {"status":"Healthy", ... "database" Healthy}
$ GET /api/v1/dashboard/indicadores (Admin)    -> 6 indicadores (volumen 466, SLA 83,41 %, backlog 26 ...)
$ GET /api/v1/reportes/gerencial/exportar?formato=xlsx|pdf|csv -> 200 en los tres formatos
$ GET /api/v1/reportes/operativo (Analista)    -> 403
```

## Reversión

```
$ git checkout 58a92bc          # commit anterior a los reportes (versión "vieja")
$ docker compose up -d --build  # reconstruye api y web con el código anterior
$ curl http://localhost:8088/health                      -> 200
$ GET /api/v1/dashboard/indicadores (Admin)              -> 200   (la versión anterior sí lo tiene)
$ GET /api/v1/reportes/operativo (Admin)                 -> 404   (el endpoint nuevo ya no existe)
```

La base de datos **no se tocó**: la API anterior arrancó sobre el mismo volumen `sqldata`, con la migración más nueva ya aplicada, y no perdió datos ni falló al migrar. El resultado es el esperado de un *rollback*: el sistema sigue sano y sin las funciones nuevas.

## Vuelta a la versión actual

```
$ git checkout main
$ docker compose up -d --build
$ GET /api/v1/reportes/operativo (Admin)   -> 200
$ docker compose ps                        -> db, api y web healthy
```

## Conclusiones

- La reversión de código por commit funciona con un solo comando y tarda unos minutos (reconstrucción de las imágenes).
- Si la versión nueva hubiera traído una migración que **cambia o borra datos**, no basta revertir el código: hay que restaurar el respaldo hecho antes de actualizar o revertir la migración (`evidencias/reversion-migracion.md`).
- Con las imágenes publicadas en GHCR (etiqueta `:<sha>`), la reversión se hace sin reconstruir: se fija la etiqueta anterior en `compose.yaml` y `docker compose up -d`. **No se ensayó en esta prueba** (se reconstruyó desde el código).
