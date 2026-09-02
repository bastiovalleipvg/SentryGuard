# SentryGuard

Proyecto ASP.NET Core / .NET 10 con Entity Framework Core y SQL Server.

## Conexión a la base de datos

La cadena de conexión **no debe contener credenciales compartidas**. Cada desarrollador debe usar su propia conexión local o la de su entorno.

En este repositorio hay un ejemplo en `SentryGuard/appsettings.json`, pero al subir el proyecto a GitHub lo recomendable es reemplazarlo por una cadena local o moverla a User Secrets / variables de entorno.

Ejemplo de conexión local:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR\TU_INSTANCIA;Database=SentryGuardDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

## Generar los modelos con scaffolding en Visual Studio

Si necesitas regenerar el `DbContext` y las entidades desde la base de datos en tu equipo:

1. Abre la solución en Visual Studio.
2. Verifica que SQL Server / SQL Express esté corriendo en tu máquina.
3. Cambia la cadena de conexión de `DefaultConnection` por tu instancia local.
4. Abre **Tools > NuGet Package Manager > Package Manager Console**.
5. Ejecuta este comando:

```powershell
Scaffold-DbContext "Server=TU_SERVIDOR\TU_INSTANCIA;Database=SentryGuardDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -Context SentryGuardDbContext -ContextDir Data -OutputDir Models -DataAnnotations -Force -NoOnConfiguring
```

### Alternativa con CLI

Si prefieres la consola:

```powershell
dotnet ef dbcontext scaffold "Server=TU_SERVIDOR\TU_INSTANCIA;Database=SentryGuardDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --context SentryGuardDbContext --context-dir Data --output-dir Models --force --no-onconfiguring --data-annotations --use-database-names
```

## Notas importantes

- El scaffolding puede sobrescribir archivos existentes si usas `-Force`.
- Si quieres conservar el código actual, haz una copia antes de regenerar.
- Los archivos generados deben quedar dentro de `SentryGuard/Data` y `SentryGuard/Models`.

## ¿Por qué no se ven `Services` y `Endpoints` en Visual Studio?

Esas carpetas **sí existen físicamente** en la solución, pero están vacías. Visual Studio normalmente no muestra carpetas vacías en Solution Explorer si no tienen archivos o si no están incluidas explícitamente en el proyecto.

Para que aparezcan tienes dos opciones:

- crear al menos un archivo dentro de cada carpeta, o
- agregarlas explícitamente al `.csproj` con entradas como:

```xml
<ItemGroup>
  <Folder Include="Endpoints\" />
  <Folder Include="Services\" />
</ItemGroup>
```

## Recomendación para el equipo

- No subir credenciales reales al repositorio.
- Cada compañero debe usar su propia cadena de conexión.
- Si van a regenerar modelos, hacerlo desde su entorno local con su SQL Server.
