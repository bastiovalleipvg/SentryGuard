@echo off
rem ==========================================================================
rem  SentryGuard - inicia la API y el frontend sin abrir un IDE.
rem
rem    iniciar.bat           modo desarrollo   (frontend en https://localhost:5173)
rem    iniciar.bat preview   build de produccion con CSP (https://localhost:4173)
rem
rem  Cada servicio se abre en su propia ventana. Para detener todo: detener.bat
rem ==========================================================================
setlocal EnableExtensions
title SentryGuard - Inicio
cd /d "%~dp0"

set "MODO=dev"
if /i "%~1"=="preview" set "MODO=preview"
set "API_URL=https://localhost:7166"
set "WEB_URL=https://localhost:5173"
if "%MODO%"=="preview" set "WEB_URL=https://localhost:4173"

echo.
echo  ============================================
echo   SentryGuard - iniciando  [modo %MODO%]
echo  ============================================
echo.

rem --- 1. Requisitos -------------------------------------------------------
where dotnet >nul 2>&1 || goto :sin_dotnet
where npm >nul 2>&1 || goto :sin_node
where curl.exe >nul 2>&1 || goto :sin_curl

rem --- 2. Secretos de la API (User Secrets, fuera del repositorio) ---------
dotnet user-secrets list --project SentryGuard 2>nul | findstr /b /c:"Jwt:Key" >nul || goto :sin_secretos

rem --- 3. Certificado HTTPS de desarrollo ----------------------------------
dotnet dev-certs https --check --trust >nul 2>&1
if not errorlevel 1 goto :cert_ok
echo [..] El certificado HTTPS de desarrollo no es de confianza. Windows pedira confirmacion.
dotnet dev-certs https --trust
:cert_ok
if exist "sentryguard-web\certs\localhost.pem" goto :cert_web_ok
echo [..] Exportando el certificado HTTPS para el frontend...
if not exist "sentryguard-web\certs" mkdir "sentryguard-web\certs"
dotnet dev-certs https --export-path "sentryguard-web\certs\localhost.pem" --format Pem --no-password >nul
:cert_web_ok

rem --- 4. Configuracion y dependencias del frontend ------------------------
if not exist "sentryguard-web\.env" copy "sentryguard-web\.env.example" "sentryguard-web\.env" >nul
if exist "sentryguard-web\node_modules" goto :deps_ok
echo [..] Instalando dependencias del frontend (solo la primera vez)...
pushd sentryguard-web
call npm install
if errorlevel 1 (popd & goto :fallo_npm)
popd
:deps_ok

rem --- 5. API --------------------------------------------------------------
curl.exe -sk -f -o nul "%API_URL%/api/v1/catalogos/roles" >nul 2>&1
if not errorlevel 1 (
  echo [ok] La API ya estaba corriendo en %API_URL%
  goto :api_lista
)

echo [..] Compilando la API...
dotnet build SentryGuard\SentryGuard.csproj -nologo -v q
if errorlevel 1 goto :fallo_build

dotnet ef --version >nul 2>&1
if errorlevel 1 (
  echo [aviso] dotnet-ef no esta instalado: no se aplican migraciones. Ver README, seccion 1.3.
) else (
  echo [..] Aplicando migraciones pendientes...
  dotnet ef database update --project SentryGuard --no-build >nul
  if errorlevel 1 goto :fallo_bd
)

echo [..] Levantando la API...
start "SentryGuard API" /D "%~dp0SentryGuard" cmd /k dotnet run --no-build --launch-profile https

set /a INTENTOS=0
:esperar_api
curl.exe -sk -f -o nul "%API_URL%/api/v1/catalogos/roles" >nul 2>&1
if not errorlevel 1 goto :api_lista
set /a INTENTOS+=1
if %INTENTOS% geq 60 goto :timeout_api
ping -n 3 127.0.0.1 >nul
goto :esperar_api
:api_lista
echo [ok] API lista en %API_URL%

rem --- 6. Frontend ---------------------------------------------------------
curl.exe -sk -f -o nul "%WEB_URL%" >nul 2>&1
if not errorlevel 1 (
  echo [ok] El frontend ya estaba corriendo en %WEB_URL%
  goto :web_listo
)

echo [..] Levantando el frontend...
if "%MODO%"=="preview" (
  start "SentryGuard Web" /D "%~dp0sentryguard-web" cmd /k "npm run build && npm run preview"
) else (
  start "SentryGuard Web" /D "%~dp0sentryguard-web" cmd /k npm run dev
)

set /a INTENTOS=0
:esperar_web
curl.exe -sk -f -o nul "%WEB_URL%" >nul 2>&1
if not errorlevel 1 goto :web_listo
set /a INTENTOS+=1
if %INTENTOS% geq 90 goto :timeout_web
ping -n 3 127.0.0.1 >nul
goto :esperar_web
:web_listo
echo [ok] Frontend listo en %WEB_URL%

if not defined SENTRYGUARD_SIN_NAVEGADOR start "" "%WEB_URL%"

echo.
echo  --------------------------------------------
echo   Aplicacion:  %WEB_URL%
echo   API:         %API_URL%
echo   Scalar:      %API_URL%/scalar/v1
echo.
echo   Usuarios de prueba (clave Admin123!):
echo     admin@sentryguard.cl       Administrador
echo     supervisor@sentryguard.cl  Supervisor
echo     analista@sentryguard.cl    Analista
echo     cliente@sentryguard.cl     Cliente
echo.
echo   Para detener todo: detener.bat
echo  --------------------------------------------
echo.
goto :fin

rem --- Errores -------------------------------------------------------------
:sin_dotnet
echo [ERROR] No se encontro el SDK de .NET. Instala .NET 10: https://dotnet.microsoft.com/download
goto :error
:sin_node
echo [ERROR] No se encontro Node.js/npm. Instala Node.js 22.15 o superior: https://nodejs.org
goto :error
:sin_curl
echo [ERROR] No se encontro curl.exe, que viene con Windows 10 y 11.
goto :error
:sin_secretos
echo [ERROR] Faltan los User Secrets de la API: Jwt:Key y la cadena de conexion.
echo         Configuralos como indica la seccion 1.1 del README.md.
goto :error
:fallo_npm
echo [ERROR] Fallo npm install en sentryguard-web.
goto :error
:fallo_build
echo [ERROR] La API no compila. Si ya estaba abierta en otra ventana, ejecuta detener.bat y reintenta.
goto :error
:fallo_bd
echo [ERROR] No se pudieron aplicar las migraciones. Revisa que SQL Server este corriendo
echo         y que la cadena de conexion de los User Secrets sea correcta.
goto :error
:timeout_api
echo [ERROR] La API no respondio en 2 minutos. Revisa la ventana "SentryGuard API".
goto :error
:timeout_web
echo [ERROR] El frontend no respondio a tiempo. Revisa la ventana "SentryGuard Web".
goto :error

:error
echo.
if not defined SENTRYGUARD_SIN_PAUSA pause
endlocal
exit /b 1

:fin
if not defined SENTRYGUARD_SIN_PAUSA pause
endlocal
exit /b 0
