# ==========================================
# Etapa 1: Build de la SPA React
# ==========================================
FROM node:22-alpine AS build
WORKDIR /app

# Copiar archivos de dependencias y restaurar
COPY package.json package-lock.json ./
RUN npm ci

# Copiar código fuente y compilar bundle de producción
COPY . .
RUN npm run build

# ==========================================
# Etapa 2: Servidor Web Nginx
# ==========================================
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
