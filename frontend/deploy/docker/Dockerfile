# syntax=docker/dockerfile:1
# Booking web app image. Build from the frontend/ folder:
#   docker build -f deploy/docker/Dockerfile -t booking-frontend:latest .

# ---- build ----
FROM node:20-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Production build uses src/environments/environment.prod.ts (apiUrl: '/api').
RUN npx ng build --configuration production

# ---- runtime ----
# nginx-unprivileged runs as a non-root user and listens on 8080.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final

COPY deploy/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/frontend/browser/ /usr/share/nginx/html/

EXPOSE 8080
