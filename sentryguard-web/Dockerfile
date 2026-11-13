# ==========================================
# Frontend de SentryGuard (React + Vite) servido por nginx sin privilegios
# ==========================================

# Etapa 1: compilar la SPA
FROM node:22.14.0-alpine3.21 AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# "same-origin": la SPA llama a la API con rutas relativas y nginx las reenvia. No se incrusta ninguna URL
# ni secreto en el bundle (toda variable VITE_* es publica).
ARG VITE_API_URL=same-origin
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# Etapa 2: nginx sin privilegios (usuario nginx, puerto 8080)
FROM nginxinc/nginx-unprivileged:1.28.0-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget -q --spider http://127.0.0.1:8080/nginx-health || exit 1
