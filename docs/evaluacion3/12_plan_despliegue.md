# 12. Plan de despliegue

Este plan describe cómo se instala, actualiza y recupera SentryGuard. Todo lo que dice se basa en archivos que existen en el repositorio: `compose.yaml`, `compose.override.yaml`, `.env.example`, `.github/workflows/ci.yml`, `scripts/` y `sentryguard-web/nginx.conf`. Lo que **no pudimos verificar** (certificado real con dominio, algunos precios) está marcado con **[NO VERIFICADO]**; los precios de la nube y la licencia de QuestPDF se consultaron el 23 de noviembre de 2026 y llevan su fuente.

## 1. Entornos

| | Desarrollo | Pruebas (CI y local) | Producción propuesta |
|---|---|---|---|
| Cómo se levanta | `dotnet run --launch-profile https` (API) y `npm run dev` (web), o `docker compose up --build` | GitHub Actions (`ci.yml`) y `dotnet test` | `docker compose up -d` en un servidor, con imágenes publicadas en GHCR |
| Base de datos | SQL Server local o el contenedor `db` (`compose.override.yaml` lo publica en `127.0.0.1:${DB_HOST_PORT}`) | `SentryGuardDB_Test`, se recrea desde las migraciones en cada corrida | Contenedor `db` sin puertos publicados, volumen `sqldata` |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Testing` (sin *rate limiting*, porque `WebApplicationFactory` comparte la IP) | `Production` |
| Secretos | User Secrets (`dotnet user-secrets`) | Variables del job de CI (clave aleatoria generada en cada suite) | `.env` en el servidor, creado con `scripts/generar-env.*` |
| Documentación de la API (`/scalar/v1`) | Habilitada | Habilitada | `DOCUMENTACION_HABILITADA=false` |
| Datos | Sintéticos (520 incidentes, contenedor `seed` o `dotnet run -- --seed`) | Solo catálogos y usuarios sembrados | Sintéticos solo en la demostración; en un uso real, sin el contenedor `seed` |
| HTTPS | Certificado de desarrollo de .NET (`npm run certs`) | No aplica | Terminación TLS en un *reverse proxy* delante de nginx (ver apartado 2) |
| IA | `IA__ApiKey` vacía: asistente simulado | Asistente simulado | `IA_API_KEY` real, o vacía para usar el simulado |

## 2. Infraestructura propuesta

- **Servidor:** una máquina virtual Linux con Docker Engine y el plugin Compose. Recursos mínimos estimados: **2 vCPU, 4 GB de RAM y 30 GB de disco**. SQL Server 2022 está limitado a 2 GB en `compose.yaml` (`mem_limit: 2g`), la API y nginx usan unos cientos de MB y el resto es margen. *(Estimación propia a partir del límite de memoria del compose; no se hizo una prueba de carga.)*
- **Puertos:** hacia afuera, solo el 443 (y el 80 para redirigir) del *reverse proxy*. El contenedor `web` se publica en `${WEB_PORT:-8088}` y debe quedar accesible únicamente desde el proxy. `api` (8080) y `db` (1433) **no se publican**: viven en redes internas de Docker (`app` y `datos`).
- **Redes:** `app` (web ↔ api) y `datos` (api ↔ db); la base de datos nunca comparte red con el servidor web.
- **Dominio y HTTPS:** nginx del contenedor `web` sirve la aplicación y hace de *proxy* de `/api/`, `/health` y `/openapi`, pero **no termina TLS**. En producción se pone delante un proxy con certificado, por ejemplo Caddy o Traefik con Let's Encrypt. Con el proxy delante, la API ya está preparada: `Proxy__ConfiarEnEncabezados=true` (lee `X-Forwarded-For`) y HSTS se activa fuera de Development. **[NO VERIFICADO]**: no se probó con un dominio ni un certificado reales; en la demostración se usa `http://localhost:8088`.

## 3. Pasos de despliegue

Con Docker instalado, en el servidor:

```bash
git clone <url-del-repositorio> sentryguard && cd sentryguard
./scripts/generar-env.sh          # crea .env con SA_PASSWORD y JWT_KEY aleatorias (en Windows: ./scripts/generar-env.ps1)
# editar .env: ASPNETCORE_ENVIRONMENT=Production, DOCUMENTACION_HABILITADA=false, IA_API_KEY=<si se usa>
docker compose pull               # si se usan las imágenes publicadas en GHCR; si no, el siguiente comando las construye
docker compose up -d --build
docker compose ps                 # db, api y web deben quedar en estado "healthy"
curl -fsS http://localhost:8088/health
```

1. `db` arranca primero y espera su `healthcheck` (`sqlcmd SELECT 1`).
2. `api` aplica las migraciones al iniciar (`Database__MigrarAlIniciar=true`) y expone `/health`.
3. `web` sirve la aplicación en el puerto `${WEB_PORT}`.
4. `seed` (opcional, una sola vez, `restart: "no"`) carga los datos sintéticos. Es idempotente: si ya hay datos, no hace nada.
5. Prueba de humo: abrir la aplicación, iniciar sesión con un usuario de prueba y abrir el dashboard.

## 4. Secretos y variables

| Variable | Dónde vive | Notas |
|---|---|---|
| `SA_PASSWORD` | `.env` del servidor | Mínimo 12 caracteres; sin `;`, `=`, `"`, `$` ni espacios (van en la cadena de conexión) |
| `JWT_KEY` | `.env` del servidor | Aleatoria de al menos 64 caracteres; si se filtra, se cambia y se reinicia la API (todas las sesiones caducan) |
| `IA_API_KEY`, `IA_MODELO` | `.env` del servidor | Opcional. La clave viaja en el encabezado `x-goog-api-key`, nunca en la URL |
| `ASPNETCORE_ENVIRONMENT`, `WEB_PORT`, `DOCUMENTACION_HABILITADA` | `.env` | Ver `.env.example` |

- `.env` está en `.gitignore` y solo se versiona `.env.example`, sin valores reales. El historial de git se limpió de una clave antigua (ADR-011).
- En desarrollo se usan User Secrets; en CI, los secretos del repositorio de GitHub (`secrets.*`), que el pipeline nunca imprime.
- `compose.yaml` **falla a propósito** si faltan `SA_PASSWORD` o `JWT_KEY`: no existe un valor por defecto.

## 5. Actualización y reversión

**Actualizar**

```bash
git pull
docker compose build           # o: docker compose pull, con las imágenes de GHCR
docker compose up -d           # recrea solo los contenedores cuya imagen cambió
docker compose ps && curl -fsS http://localhost:8088/health
```

- El pipeline publica en GHCR solo desde `main` y solo si pasaron backend, frontend, contenedores y el análisis Trivy. Cada imagen lleva dos etiquetas: el hash del commit (`:<sha>`) y `:latest`.
- Las migraciones se aplican solas al iniciar la API. Antes de actualizar, **siempre se hace un respaldo** (apartado 6).

**Revertir (rollback)**

1. *Código*: volver a la imagen anterior. Se fija la etiqueta del commit bueno en `compose.yaml` (o se hace `git checkout <commit-anterior>` y `docker compose up -d --build`).
2. *Base de datos*: si la versión nueva trajo una migración, hay dos caminos:
   - Revertir la migración: `dotnet ef database update <migracion-anterior>` (usa el método `Down` de cada migración).
   - Restaurar el respaldo hecho antes de actualizar (el camino más seguro si la migración borró o transformó datos).
3. Verificar con `/health` y con una consulta de humo.

**Prueba realizada** (el 23 de noviembre de 2026, base de desarrollo): se retrocedió la migración `AgregaRestriccionesCheck` y se volvió a aplicar sin errores. La evidencia, con la salida de los comandos, está en `docs/evaluacion3/evidencias/reversion-migracion.md`. **También se probó con contenedores** (reversión de la versión del código sobre la misma base de datos): ver `docs/evaluacion3/evidencias/reversion-imagen.md`.

## 6. Respaldo y recuperación

- **Frecuencia:** un respaldo completo cada día (RPO máximo de **24 horas**) y uno antes de cada actualización.
- **Cómo:** `scripts/respaldo-restauracion.ps1` ejecuta `BACKUP DATABASE ... WITH CHECKSUM, COMPRESSION` dentro del contenedor, verifica el archivo con `RESTORE VERIFYONLY`, lo restaura con otro nombre y **compara el número de filas** de tablas y vistas con la base original. Deja el `.bak` en `./backups/` y la evidencia en `docs/evidencias/respaldo-restauracion.txt`.
- **Programación:** una tarea programada del servidor (cron o Programador de tareas) que ejecute el script cada noche; los `.bak` deben copiarse **fuera del servidor** y conservarse 7 días.
- **Tiempo máximo de restauración (RTO):** **30 minutos**, medido en la prueba de `docs/evidencias/respaldo-restauracion.txt` para una base de este tamaño (520 incidentes).
- **Restaurar:** detener `api`, ejecutar `RESTORE DATABASE SentryGuard FROM DISK = ... WITH REPLACE` en el contenedor `db`, iniciar `api` y verificar `/health`.

## 7. Monitoreo y logs

| Qué | Cómo | Dónde se ve |
|---|---|---|
| Disponibilidad | `GET /health` (API) y `/nginx-health`; `healthcheck` de Docker en `db` y `api`; `restart: unless-stopped` | `docker compose ps` |
| Errores 5xx y trazas | `ProblemDetails` con `traceId` en cada error; el *middleware* de errores registra la excepción con ese mismo `traceId` | `docker compose logs api` |
| Rendimiento | Meta: listados < 500 ms (p95) y dashboard < 1,5 s (RNF-01) | Revisión manual con la consola del navegador o `curl -w "%{time_total}"` |
| Seguridad | Intentos de login fallidos (bloqueo tras 5), respuestas 429, auditoría de cada escritura y exportación | `GET /api/v1/auditoria` (solo Administrador) |
| Vulnerabilidades | Trivy en cada ejecución del pipeline; Dependabot semanal | Pestaña *Actions* y *Pull requests* de GitHub |

- **Rotación de logs:** `json-file` con `max-size: 10m` y `max-file: 3` en cada contenedor (ya está en `compose.yaml`).
- **Alertas:** no hay un sistema de alertas automático (**limitación declarada**). Propuesta para una versión real: un monitor externo de disponibilidad (por ejemplo, UptimeRobot) que consulte `/health` y avise por correo.

## 8. Seguridad en producción

- **Cabeceras:** la API envía `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` y `Content-Security-Policy` en `/api`; nginx añade además `Permissions-Policy` y su propia CSP estricta (`default-src 'self'`, `frame-ancestors 'none'`). HSTS se activa solo fuera de Development.
- **CORS:** restringido a los orígenes de `Cors:AllowedOrigins`; los demás no reciben `Access-Control-Allow-Origin`. En producción con nginx, la SPA y la API comparten origen.
- **Endurecimiento de contenedores:** imágenes con versión fija, `read_only: true`, `tmpfs` para `/tmp`, `cap_drop: [ALL]`, `no-new-privileges`, usuario sin privilegios y la base de datos sin puertos publicados.
- **Limitación de tasa:** login 5/min, crear incidente 10/min, IA 15/min y exportaciones 20/min, por IP o por usuario.
- **Actualización de imágenes y paquetes:** Dependabot propone las actualizaciones de NuGet, npm, Docker y GitHub Actions; Trivy escanea las imágenes en cada pipeline. Se revisan semanalmente; **las versiones mayores de .NET no se fusionan** sin una prueba completa.
- **Datos:** solo sintéticos; la IA recibe únicamente cifras agregadas o el texto del incidente, nunca datos de usuarios.

## 9. Costos y riesgos

**Costos mensuales estimados** (precios consultados el 23 de noviembre de 2026 en las páginas de los proveedores, a través de un buscador; **confirmar en la página del proveedor antes de contratar**, porque cambian)

| Ítem | Estimación | Fuente |
|---|---|---|
| VM Linux de 2 vCPU y 4 GB, opción de nube conocida (DigitalOcean, plan *Basic*) | unos **24 USD al mes** (otras variantes, desde 32 hasta unos 38 USD) | [digitalocean.com/pricing/droplets](https://www.digitalocean.com/pricing/droplets) |
| La misma VM en un proveedor económico (Hetzner, línea CX) | entre **3,79 y 4,51 EUR al mes** según la fuente (con o sin IVA); el plan CX22 figura como reemplazado por otro más nuevo **[los precios no coinciden entre fuentes: sin verificar]** | [hetzner.com/news/new-cx-plans](https://www.hetzner.com/news/new-cx-plans) |
| Respaldos del proveedor | 20 % del costo de la VM los semanales y 30 % los diarios (DigitalOcean); aquí los respaldos los hace el script, así que es opcional | [digitalocean.com/pricing](https://www.digitalocean.com/pricing/droplets) |
| Dominio | unos 10 a 15 USD al año **[estimación propia, sin fuente]** | — |
| Certificado HTTPS (Let's Encrypt) | 0 USD | — |
| Licencia de SQL Server | Edición *Developer* (no productiva) o *Express* en esta etapa; una edición *Standard* tiene costo de licencia **[no verificado]** | — |
| API de IA | Variable según el uso; con la clave vacía el sistema funciona con el asistente simulado a costo 0 | — |
| QuestPDF | **0 USD**: la licencia *Community* (v3.0) es gratuita para organizaciones con ingresos brutos anuales **menores a 1 000 000 USD** y no aplica a entidades públicas ni a empresas que cotizan en bolsa; este proyecto es académico. Si el sistema se usara en una organización que no cumpla esas condiciones, hay que comprar una licencia | [questpdf.com/license/community.html](https://questpdf.com/license/community.html) |

**Costo total estimado para una puesta en marcha mínima:** entre unos 5 y 30 USD al mes, según el proveedor, más el dominio.

**Riesgos**

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Pérdida de datos por falla del disco | Media | Alto | Respaldo diario con verificación y copia externa; restauración ensayada |
| Una migración daña datos | Baja | Alto | Respaldo antes de actualizar; probar la migración en pruebas; `Down` ensayado |
| Filtración de una clave | Baja | Alto | Solo en `.env`; rotación documentada; `.gitignore` y revisión del historial |
| Caída del proveedor de IA | Media | Bajo | La API responde 502/504 con el formato estándar y el formulario sigue funcionando sin la sugerencia |
| Dependencia vulnerable | Media | Medio | Dependabot, Trivy y `npm audit` en el pipeline |
| Un solo servidor (sin alta disponibilidad) | Alta | Medio | Aceptado en esta etapa; `restart: unless-stopped` y recuperación en 30 minutos |

## 10. Lista de verificación previa a salir a producción

- [ ] `.env` creado con `generar-env`, con claves aleatorias y **sin** subirlo al repositorio (`git check-ignore .env`).
- [ ] `ASPNETCORE_ENVIRONMENT=Production` y `DOCUMENTACION_HABILITADA=false`.
- [ ] El pipeline de `main` está en verde (backend, frontend, contenedores, Trivy).
- [ ] `docker compose ps` muestra `db`, `api` y `web` en estado *healthy*; `/health` responde 200.
- [ ] Puertos 1433 y 8080 **no** expuestos fuera de Docker.
- [ ] HTTPS activo en el proxy y HSTS presente (`curl -I`).
- [ ] Respaldo realizado y **restauración ensayada** (`scripts/respaldo-restauracion.ps1`).
- [ ] Rollback ensayado (imagen anterior y migración anterior).
- [ ] Prueba de humo por rol: Administrador, Supervisor, Analista y Cliente inician sesión y ven solo lo suyo.
- [ ] Dashboard y un reporte exportado a Excel, PDF y CSV, con registro en la auditoría.
- [ ] Credenciales de prueba (`Admin123!`) cambiadas o usuarios de prueba desactivados.
- [ ] Datos sintéticos: el contenedor `seed` se ejecutó solo para la demostración.
