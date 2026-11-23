#!/usr/bin/env bash
# Crea .env con claves aleatorias (SA_PASSWORD y JWT_KEY) a partir de .env.example.
# No pisa un .env existente salvo que se pase --forzar.
set -euo pipefail
raiz="$(cd "$(dirname "$0")/.." && pwd)"
destino="$raiz/.env"
if [ -f "$destino" ] && [ "${1:-}" != "--forzar" ]; then
  echo ".env ya existe. Usa --forzar para reemplazarlo (se pierde la contraseña de SA actual)."
  exit 0
fi
aleatorio() { LC_ALL=C tr -dc "$2" < /dev/urandom | head -c "$1"; }
# SQL Server exige complejidad: mayusculas, minusculas y numeros
sa="$(aleatorio 8 'A-HJ-NP-Z')$(aleatorio 8 'a-km-z')$(aleatorio 8 '2-9')"
jwt="$(aleatorio 64 'A-Za-z0-9')"
sed -e "s|^SA_PASSWORD=.*|SA_PASSWORD=$sa|" -e "s|^JWT_KEY=.*|JWT_KEY=$jwt|" "$raiz/.env.example" > "$destino"
echo ".env creado con claves aleatorias. Siguiente paso:  docker compose up --build"
