<#
.SYNOPSIS
  Respalda la base de SentryGuard (SQL Server en Docker), la restaura con otro nombre y compara los datos.

.DESCRIPTION
  1. BACKUP DATABASE completo con CHECKSUM y compresión -> SentryGuard.bak
  2. RESTORE VERIFYONLY (el archivo es legible y no está corrupto)
  3. RESTORE DATABASE como SentryGuard_Restaurada (con MOVE a archivos nuevos)
  4. Compara el número de filas de tablas y vistas entre la base original y la restaurada
  5. Elimina la base restaurada y copia el .bak a ./backups/
  El resultado queda en docs/evidencias/respaldo-restauracion.txt (evidencia del Hito 3).

.EXAMPLE
  $env:SA_PASSWORD = '<contraseña de sa>'; ./scripts/respaldo-restauracion.ps1
#>
param(
    [string]$Contenedor = 'sentryguard-db',
    [string]$BaseDatos = 'SentryGuard',
    [string]$Salida = (Join-Path $PSScriptRoot '..' 'backups'),
    [string]$Evidencia = (Join-Path $PSScriptRoot '..' 'docs' 'evidencias' 'respaldo-restauracion.txt')
)

$ErrorActionPreference = 'Stop'
if (-not $env:SA_PASSWORD) { throw 'Define la variable de entorno SA_PASSWORD (la misma del compose).' }

$restaurada = "${BaseDatos}_Restaurada"
$rutaBak = '/var/opt/mssql/backups/SentryGuard.bak'
$lineas = [System.Collections.Generic.List[string]]::new()

function Escribir([string]$texto) { Write-Host $texto; $lineas.Add($texto) }

function Sql([string]$consulta, [string]$base = 'master') {
    $salida = docker exec -e "SQLCMDPASSWORD=$($env:SA_PASSWORD)" $Contenedor /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -C -b -d $base -W -h -1 -s '|' -Q $consulta 2>&1
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd falló: $salida" }
    return $salida
}

Escribir "Respaldo y restauración de $BaseDatos - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') (hora local)"
Escribir "Contenedor: $Contenedor | SQL Server: $((Sql 'SET NOCOUNT ON; SELECT @@VERSION' | Select-Object -First 1))"
Escribir ''

# 1. Respaldo completo
docker exec $Contenedor mkdir -p /var/opt/mssql/backups | Out-Null
Escribir '1. BACKUP DATABASE (completo, CHECKSUM, COMPRESSION)'
Sql "BACKUP DATABASE [$BaseDatos] TO DISK = N'$rutaBak' WITH INIT, FORMAT, CHECKSUM, COMPRESSION, NAME = N'SentryGuard completo'" | Out-Null
$tamano = (Sql "SET NOCOUNT ON; SELECT CAST(compressed_backup_size/1048576.0 AS DECIMAL(10,2)) FROM msdb.dbo.backupset WHERE backup_set_id = (SELECT MAX(backup_set_id) FROM msdb.dbo.backupset WHERE database_name = N'$BaseDatos')" | Select-Object -First 1).Trim()
Escribir "   OK. Archivo $rutaBak ($tamano MB comprimido)"

# 2. Verificación
Escribir '2. RESTORE VERIFYONLY'
Sql "RESTORE VERIFYONLY FROM DISK = N'$rutaBak' WITH CHECKSUM" | Out-Null
Escribir '   OK. El respaldo es legible y su suma de verificación coincide.'

# 3. Restauración con otro nombre
Escribir "3. RESTORE DATABASE como $restaurada"
$archivos = Sql "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'$rutaBak'" | Where-Object { $_ -match '\|' }
$mover = foreach ($f in $archivos) {
    $c = $f -split '\|'
    $logico = $c[0].Trim(); $tipo = $c[2].Trim()
    $ext = if ($tipo -eq 'L') { 'ldf' } else { 'mdf' }
    "MOVE N'$logico' TO N'/var/opt/mssql/data/${restaurada}_$logico.$ext'"
}
Sql "IF DB_ID(N'$restaurada') IS NOT NULL DROP DATABASE [$restaurada]; RESTORE DATABASE [$restaurada] FROM DISK = N'$rutaBak' WITH CHECKSUM, REPLACE, $($mover -join ', ')" | Out-Null
Escribir '   OK. Base restaurada.'

# 4. Comparación de datos
Escribir '4. Comparación de filas (original vs restaurada)'
$objetos = 'tb_Rol', 'tb_Organizacion', 'tb_Usuario', 'tb_Activo', 'tb_Incidente', 'tb_BitacoraIncidente',
           'tb_EvidenciaIncidente', 'tb_LogAuditoria', 'tb_CategoriaIncidente', 'tb_TransicionEstado',
           'vw_IncidenteDetalle', 'vw_CumplimientoSla', 'vw_IndicadoresMensuales'
$diferencias = 0
foreach ($o in $objetos) {
    $a = [int]((Sql "SET NOCOUNT ON; SELECT COUNT(*) FROM $o" $BaseDatos) | Select-Object -First 1).Trim()
    $b = [int]((Sql "SET NOCOUNT ON; SELECT COUNT(*) FROM $o" $restaurada) | Select-Object -First 1).Trim()
    $estado = if ($a -eq $b) { 'igual' } else { $diferencias++; 'DIFERENTE' }
    Escribir ('   {0,-26} {1,8} {2,8}  {3}' -f $o, $a, $b, $estado)
}

# 5. Limpieza y entrega del archivo
Sql "DROP DATABASE [$restaurada]" | Out-Null
New-Item -ItemType Directory -Force $Salida | Out-Null
docker cp "${Contenedor}:$rutaBak" (Join-Path $Salida 'SentryGuard.bak') | Out-Null
Escribir "5. Base restaurada eliminada. Copia local: $(Join-Path $Salida 'SentryGuard.bak')"
Escribir ''
Escribir $(if ($diferencias -eq 0) { 'RESULTADO: restauración limpia, todos los conteos coinciden.' } else { "RESULTADO: FALLÓ, $diferencias objetos difieren." })

New-Item -ItemType Directory -Force (Split-Path $Evidencia) | Out-Null
$lineas | Set-Content -Path $Evidencia -Encoding utf8
if ($diferencias -gt 0) { exit 1 }
