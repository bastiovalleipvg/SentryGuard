# SentryGuard — CyberOps Chile SpA

API RESTful para gestión de incidentes de ciberseguridad, construida con ASP.NET Core
(.NET 10) + Entity Framework Core (Code-First) + SQL Server + JWT.

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express, Developer o LocalDB) corriendo localmente
- Herramienta de EF Core (si no la tienes):
  ```
  dotnet tool install --global dotnet-ef
  ```
- Git

## 1. Clonar el repositorio

```
git clone https://github.com/<usuario>/SentryGuard.git
cd SentryGuard
```

## 2. Configurar credenciales (obligatorio, no están en el repo)

Por diseño, `appsettings.json` **no contiene** la clave JWT ni la cadena de conexión reales
— están vacías a propósito. Cada desarrollador configura las suyas localmente con
[User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), que las
guarda fuera de la carpeta del proyecto y nunca se suben a git.

```
cd SentryGuard
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=TU_SERVIDOR\TU_INSTANCIA;Database=SentryGuardDB;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_CARACTERES"
```

Para generar una clave aleatoria segura en PowerShell:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

Si al correr la app ves el error *"Falta configurar Jwt:Key..."*, es porque falta este
paso — no porque algo esté roto.

> **Nota:** esto solo aplica al entorno `Development`. La suite de tests de integración
> usa su propia clave no sensible en `appsettings.Testing.json` (ver sección Tests) y no
> depende de este paso.

## 3. Aplicar las migraciones (crea la base de datos)

```
dotnet ef database update
```

Esto crea la base de datos `SentryGuardDB` si no existe, todas las tablas según el modelo
de EF Core, y siembra automáticamente (vía migraciones con `HasData()`, no un script
aparte) los catálogos y usuarios de prueba:

| Usuario | Rol | Contraseña |
|---|---|---|
| `admin@sentryguard.cl` | Administrador | `Admin123!` |
| `supervisor@sentryguard.cl` | Supervisor | `Admin123!` |
| `analista@sentryguard.cl` | Analista | `Admin123!` |
| `cliente@sentryguard.cl` | Cliente | `Admin123!` |

Si necesitas partir de cero (por ejemplo, tras algún problema local), puedes borrar y
recrear la base:

```
dotnet ef database drop
dotnet ef database update
```

## 4. Correr la aplicación

```
dotnet run
```

Por defecto queda disponible en `https://localhost:5170` (o el puerto que indique la
consola). En entorno `Development`:

- Documentación interactiva (Scalar): `https://localhost:5170/scalar/v1`
- Documento OpenAPI crudo: `https://localhost:5170/openapi/v1.json`

Para probar un endpoint protegido desde Scalar: haz `POST /api/v1/usuarios/login` con uno
de los usuarios de la tabla de arriba, copia el `token` de la respuesta, ábrelo en el panel
**Authentication** (arriba a la derecha en Scalar) y pégalo en el campo Bearer Token — queda
guardado entre recargas mientras pruebas otros endpoints.

## 5. Correr los tests de integración

```
dotnet test SentryGuard.slnx
```

Los tests levantan la aplicación completa (`WebApplicationFactory<Program>`) contra una
base de datos separada y descartable (`SentryGuardDB_Test`), que se recrea desde las
migraciones antes de cada corrida — no requiere ningún setup manual ni depende de los
User Secrets configurados en el paso 2.

## Variables de configuración

| Clave | Dónde va | Sensible |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | User Secrets (dev) / variable de entorno (prod) | Sí |
| `Jwt:Key` | User Secrets (dev) / variable de entorno (prod) | Sí |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpireMinutes` | `appsettings.json` | No |
| `Cors:AllowedOrigins` | `appsettings.json` | No |

En un entorno de producción real (no cubierto por este entregable académico), las claves
sensibles se configurarían como variables de entorno del sistema o del servicio de hosting
(`Jwt__Key`, `ConnectionStrings__DefaultConnection`, con doble guion bajo — así es como
ASP.NET Core mapea variables de entorno a la jerarquía de configuración), nunca en un
archivo dentro del repositorio.

## Estructura del proyecto

```
SentryGuard/
├── SentryGuard/              # API principal
│   ├── Models/                # Entidades EF Core
│   ├── Data/                  # DbContext
│   ├── Repositories/          # Patrón Repository
│   ├── Services/              # AuthService (JWT, hashing)
│   ├── Endpoints/              # Minimal APIs por recurso
│   ├── Middleware/             # Logging y manejo de excepciones
│   └── Migrations/             # Migraciones de EF Core
├── SentryGuard.Tests/         # Pruebas de integración (xUnit)
└── SentryGuard.slnx
```
