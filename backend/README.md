# booking-app — Backend (.NET 8, SQLite, JWT)

Backend for booking-app implemented with .NET 8. Project is split into three projects/solutions:

- API: HTTP endpoints and startup (src/booking.api)
- Core: domain and application logic (src/booking.core)
- Infrastructure: persistence, EF Core, Identity (src/booking.infrastructure)

This README explains prerequisites, setup, environment variables and common commands to build, run and publish the backend.

## Prerequisites

- .NET 8 SDK (download from https://dotnet.microsoft.com)
- dotnet-ef tool (for migrations): dotnet tool install --global dotnet-ef
- SQLite (optional: for inspecting DB files). On mac: brew install sqlite
- Git

Confirm:

```bash
dotnet --version   # should be 8.x
dotnet ef --version
sqlite3 --version  # optional
```

## Run (development)

Run the API project directly:

```bash
# from backend root
dotnet restore
dotnet build
dotnet run --project src/booking.api/booking.api.csproj
```

The server will listen on the configured Kestrel URLs (see appsettings or console output). Ensure the environment variables (connection string, JWT secret) are set before running.

## Run tests

If there are test projects:

```bash
dotnet test
```
