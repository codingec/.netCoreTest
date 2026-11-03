# syntax=docker/dockerfile:1
# Booking API image. Build from the backend/ folder:
#   docker build -f deploy/docker/Dockerfile -t booking-backend:latest .

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY src/booking.core/booking.core.csproj src/booking.core/
COPY src/booking.infrastructure/booking.infrastructure.csproj src/booking.infrastructure/
COPY src/booking.api/booking.api.csproj src/booking.api/
RUN dotnet restore src/booking.api/booking.api.csproj

COPY src/ src/
RUN dotnet publish src/booking.api/booking.api.csproj \
    -c Release -o /out/booking.api --no-restore /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# DbInitializerHostedService looks for schema.sql at
# <ContentRoot>/../booking.infrastructure/Persistence/Sql/schema.sql,
# so the app runs from /app/booking.api and the schema sits next to it.
WORKDIR /app/booking.api
COPY --from=build /out/booking.api ./
COPY src/booking.infrastructure/Persistence/Sql/schema.sql \
     /app/booking.infrastructure/Persistence/Sql/schema.sql

# SQLite database lives in /data (an emptyDir volume in Kubernetes).
RUN mkdir -p /data && chown "$APP_UID" /data

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/data/app.db"

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "booking.api.dll"]
