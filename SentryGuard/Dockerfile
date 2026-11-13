# ==========================================
# Etapa 1: Build de la API .NET
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY ["SentryGuard/SentryGuard.csproj", "SentryGuard/"]
RUN dotnet restore "SentryGuard/SentryGuard.csproj"

# Copiar el resto del código y compilar en Release
COPY ["SentryGuard/", "SentryGuard/"]
WORKDIR "/src/SentryGuard"
RUN dotnet publish "SentryGuard.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Etapa 2: Runtime ligero y seguro
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Usuario no root para mitigación de seguridad en contenedores
USER $APP_UID

ENTRYPOINT ["dotnet", "SentryGuard.dll"]
