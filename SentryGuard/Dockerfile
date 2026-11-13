# ==========================================
# API de SentryGuard (.NET 10), construccion en dos etapas
# Contexto de construccion: la raiz del repositorio.
# ==========================================

# Etapa 1: compilar y publicar (SDK completo, no llega a la imagen final)
FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS build
WORKDIR /src

# El csproj primero: la restauracion de paquetes queda en cache mientras no cambien las dependencias
COPY SentryGuard/SentryGuard.csproj SentryGuard/
RUN dotnet restore SentryGuard/SentryGuard.csproj

COPY SentryGuard/ SentryGuard/
RUN dotnet publish SentryGuard/SentryGuard.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Etapa 2: runtime "chiseled-extra" (sin shell ni gestor de paquetes, usuario sin privilegios). La variante
# "extra" trae ICU: Microsoft.Data.SqlClient no funciona en el modo invariante de globalizacion.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble-chiseled-extra AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY --from=build /app/publish .

EXPOSE 8080
USER $APP_UID

# La imagen no trae curl ni shell: la sonda de salud es la propia aplicacion (--healthcheck, ver Program.cs)
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=5 \
    CMD ["dotnet", "SentryGuard.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "SentryGuard.dll"]
