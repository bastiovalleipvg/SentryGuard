<#
.SYNOPSIS
  Crea .env con claves aleatorias (SA_PASSWORD y JWT_KEY) a partir de .env.example.
  No pisa un .env existente salvo que se use -Forzar.
#>
param([switch]$Forzar)

$raiz = Split-Path $PSScriptRoot -Parent
$destino = Join-Path $raiz '.env'
if ((Test-Path $destino) -and -not $Forzar) {
    Write-Host ".env ya existe. Usa -Forzar para reemplazarlo (se pierde la contraseña de SA actual)."
    exit 0
}

function Aleatorio([int]$largo, [string]$alfabeto) {
    -join (1..$largo | ForEach-Object { $alfabeto[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($alfabeto.Length)] })
}

# SQL Server exige complejidad: se garantiza al menos una mayuscula, una minuscula y un numero
$sa = (Aleatorio 8 'ABCDEFGHJKLMNPQRSTUVWXYZ') + (Aleatorio 8 'abcdefghijkmnopqrstuvwxyz') + (Aleatorio 8 '23456789')
$sa = -join ($sa.ToCharArray() | Sort-Object { [System.Security.Cryptography.RandomNumberGenerator]::GetInt32(1000) })
$jwt = Aleatorio 64 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'

(Get-Content (Join-Path $raiz '.env.example')) |
    ForEach-Object { $_ -replace '^SA_PASSWORD=.*', "SA_PASSWORD=$sa" -replace '^JWT_KEY=.*', "JWT_KEY=$jwt" } |
    Set-Content -Path $destino -Encoding utf8

Write-Host ".env creado con claves aleatorias. Siguiente paso:  docker compose up --build"
