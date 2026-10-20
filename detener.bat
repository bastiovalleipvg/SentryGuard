@echo off
rem ==========================================================================
rem  SentryGuard - detiene la API y el frontend iniciados con iniciar.bat.
rem ==========================================================================
setlocal EnableExtensions
title SentryGuard - Detener
cd /d "%~dp0"

echo.
echo  Deteniendo SentryGuard...
echo.

rem 1. Cierra las ventanas abiertas por iniciar.bat junto con sus procesos hijos.
taskkill /FI "WINDOWTITLE eq SentryGuard API*" /T /F >nul 2>&1 && echo [ok] Ventana de la API cerrada.
taskkill /FI "WINDOWTITLE eq SentryGuard Web*" /T /F >nul 2>&1 && echo [ok] Ventana del frontend cerrada.

rem 2. Respaldo: cualquier proceso que siga escuchando en los puertos de SentryGuard
rem    (por ejemplo, si se inicio a mano con dotnet run o npm run dev).
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$puertos = 7166, 5170, 5173, 4173;" ^
  "$ids = Get-NetTCPConnection -LocalPort $puertos -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique;" ^
  "foreach ($id in $ids) { $p = Get-Process -Id $id -ErrorAction SilentlyContinue; if ($p) { Write-Host ('[ok] Detenido ' + $p.ProcessName + ' (PID ' + $id + ')'); Stop-Process -Id $id -Force -ErrorAction SilentlyContinue } }"

rem 3. Verificacion
ping -n 2 127.0.0.1 >nul
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "if (Get-NetTCPConnection -LocalPort 7166, 5170, 5173, 4173 -State Listen -ErrorAction SilentlyContinue) { Write-Host '[aviso] Algun puerto sigue ocupado. Cierra las ventanas manualmente.'; exit 1 } else { Write-Host '[ok] SentryGuard detenido: los puertos 7166, 5170, 5173 y 4173 estan libres.' }"

echo.
if not defined SENTRYGUARD_SIN_PAUSA pause
endlocal
