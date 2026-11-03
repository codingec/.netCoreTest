# Backend Documentation (booking-app)

This document explains how the backend is built, what each file does, and the .NET / ASP.NET Core / EF Core concepts the code relies on. When a pattern repeats across classes (for example `CancellationToken`, tuple return values, or `AsNoTracking`), it is explained **once**, in the [.NET concepts](#4-net-concepts-used-in-this-codebase) section, and other sections link back to it.

---

## Contents

1. [Tech stack](#1-tech-stack)
2. [Solution layout](#2-solution-layout)
3. [Request lifecycle](#3-request-lifecycle-from-http-to-database)
4. [.NET concepts used in this codebase](#4-net-concepts-used-in-this-codebase)
5. [File-by-file reference](#5-file-by-file-reference)
6. [Database: `schema.sql` and startup bootstrapping](#6-database-schemasql-and-startup-bootstrapping)
7. [Authentication and authorization](#7-authentication-and-authorization)
8. [API endpoints](#8-api-endpoints)
9. [Running the backend](#9-running-the-backend)
10. [Known issues and things to watch](#10-known-issues-and-things-to-watch)

---

## 1. Tech stack

| Piece | Version / package | Used for |
|---|---|---|
| .NET | `net8.0` | Runtime and SDK for all projects |
| ASP.NET Core Web API | `Microsoft.NET.Sdk.Web` | HTTP endpoints (controllers) |
| Entity Framework Core | `Microsoft.EntityFrameworkCore` 9.0.10 | ORM (C# classes ⇄ database tables) |
| SQLite provider | `Microsoft.EntityFrameworkCore.Sqlite` 9.0.10 | Database is a single file, `app.db` |
| JWT | `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt` | Issuing and validating login tokens |
| Password hashing | `Microsoft.AspNetCore.Identity` (`PasswordHasher<T>` only) | Hashing and verifying passwords |
| Validation | `FluentValidation.AspNetCore` | Validating request bodies (`RegisterRequest`) |
| API docs | `Swashbuckle.AspNetCore` | Swagger UI at `/swagger` |
| Tests | xUnit, Moq, FluentAssertions, EF InMemory | Test project (only a placeholder test so far) |

---

## 2. Solution layout

```
backend/
├── booking.sln                         ← Solution file that groups all projects
├── src/
│   ├── booking.api/                    ← Web layer: Program.cs, controllers, validators, config
│   ├── booking.core/                   ← Domain layer: entities (Models), DTOs, constants
│   └── booking.infrastructure/         ← Data layer: DbContext, services, schema.sql, seeding
└── test/
    └── booking.api.test/               ← xUnit test project
```

The projects reference each other in one direction:

```
booking.api  ──►  booking.infrastructure  ──►  booking.core
      └──────────────────────────────────────►  booking.core
```

- **`booking.core`** has no dependencies. It holds plain C# classes and records that every other layer shares.
- **`booking.infrastructure`** knows about the database (EF Core, SQLite) and contains the business logic (services).
- **`booking.api`** wires everything together (dependency injection, auth, Swagger) and exposes HTTP endpoints.

This is a simplified "Clean Architecture" or "Onion" layout. The service interfaces (`IAuthService`, …) live in `infrastructure` next to their implementations rather than in `core`, which is a simplification of the pattern.

`Class1.cs` in `booking.core` and `booking.infrastructure` are empty template files and can be deleted.

---

## 3. Request lifecycle (from HTTP to database)

Example: `POST /api/auth/login`

```
HTTP request
   │
   ▼
Kestrel (built-in web server)
   │
   ▼
Middleware pipeline (Program.cs, in order):
   Swagger → CORS → HTTPS redirect → Authentication → Authorization
   │
   ▼
Routing → AuthController.Login(...)
   │  ├─ [FromBody]  → JSON body deserialized into LoginRequest
   │  ├─ [FromServices] → IAuthService resolved from DI
   │  └─ CancellationToken → bound to HttpContext.RequestAborted
   ▼
AuthService.LoginAsync(email, password, ct)
   │
   ▼
AppDbContext (EF Core) → LINQ translated to SQL → SQLite (app.db)
   │
   ▼
Tuple (ok, data, code) returned → controller maps it to 200 / 401
   │
   ▼
JSON response: { "code": "OK", "data": { token, expiresAt, ... } }
```

---

## 4. .NET concepts used in this codebase

### 4.1 `CancellationToken ct`: what it is and why every method has one

You asked about this signature in `AuthService`:

```csharp
public async Task<(bool ok, AuthResponse? data, string? code)> LoginAsync(
    string email, string password, CancellationToken ct)
```

**What it is.** A `CancellationToken` is a small struct that carries a signal meaning "the caller no longer needs this result, stop working." It does not stop anything by itself. Code that receives it must either check it or pass it to other async methods that check it.

**Where it comes from here.** In the controller:

```csharp
public async Task<IActionResult> Login([FromServices] IAuthService auth,
                                       [FromBody] LoginRequest dto,
                                       CancellationToken ct)
```

ASP.NET Core sees a `CancellationToken` parameter on an action method and **automatically binds it to `HttpContext.RequestAborted`**. You never send it from the client. That token is triggered when:

- the client closes the connection (browser tab closed, user navigates away, HTTP client timeout), or
- the server is shutting down.

**Where it goes.** The token is passed down the call chain:

```
Controller(ct) → AuthService.LoginAsync(..., ct) → EF Core: FirstOrDefaultAsync(..., ct)
                                                          ToListAsync(ct)
                                                          SaveChangesAsync(ct)
```

EF Core passes it on to the database driver. If the token fires while a query is running, the query is aborted and an `OperationCanceledException` is thrown. ASP.NET Core recognizes that exception for an aborted request and does not treat it as a server error.

**Why it is worth having:**

- **Saves resources.** Without it, if a user clicks "Login" and closes the tab, the server would still finish every query, hash check and JWT creation for a response nobody will read. Under load, abandoned work like that piles up.
- **Faster, cleaner shutdown.** Pending work stops when the app is stopping.
- **Standard .NET convention.** Almost every async .NET API (`ToListAsync`, `ReadAllTextAsync`, `HttpClient.GetAsync`, …) takes an optional token, so accepting and forwarding one is the expected style.

**Where it appears in this project** (the same idea in every case, so it is not repeated elsewhere in this doc):

| Location | Source of the token |
|---|---|
| Every controller action (`AuthController`, `BookingController`, `OfficeController`, `ServicesController`) | `HttpContext.RequestAborted`, bound automatically |
| Every service method (`AuthService`, `BookingService`, `OfficeService`, `ServicesService`) | Forwarded from the controller |
| `DbInitializerHostedService.StartAsync(CancellationToken cancellationToken)` | Supplied by the .NET host; fires if startup is aborted |
| `DatabaseBootstrapper.InitAsync(..., CancellationToken ct = default)` | Forwarded from the hosted service. `= default` makes it optional (`CancellationToken.None`, which never cancels) |

**Note:** `email` and `password` are not query parameters. The token is not part of the SQL query or the HTTP request. It is a control signal that runs beside the work.

**One CPU-bound gap:** in `BookingService.GetAvailability`, the `for` loop that builds time slots never checks the token. That is fine because the loop is short. For long loops the usual pattern is `ct.ThrowIfCancellationRequested();` inside the loop.

---

### 4.2 `async` / `await` and `Task<T>`

Every service and controller method is `async` and returns `Task<...>`. While the database is working, the request thread goes back to the thread pool instead of blocking, so the server can handle many more concurrent requests. Methods ending in `Async` (`FirstOrDefaultAsync`, `SaveChangesAsync`) are the awaitable EF Core versions.

### 4.3 Tuple return values: `(bool ok, T? data, string code, string? message)`

Instead of throwing exceptions for expected failures such as "not found" or "wrong password", the services return a **C# value tuple**:

```csharp
Task<(bool ok, AuthResponse? data, string? code)>
```

The controller then **deconstructs** it:

```csharp
var (ok, data, code) = await auth.LoginAsync(dto.Email, dto.Password, ct);
```

- `ok`: success flag
- `data`: the payload (`?` means it is nullable, so `null` on failure)
- `code`: a machine-readable code from `Codes.cs` (e.g. `ERR_NOT_FOUND`)
- `message`: an optional human-readable text (on create, update and delete)

This is a lightweight version of the "Result pattern". The service never decides HTTP status codes. The controller translates `code` into `200`, `400`, `401` or `404`.

### 4.4 Dependency Injection (DI) and service lifetimes

`Program.cs` registers services:

```csharp
builder.Services.AddDbContext<AppDbContext>(...);                // Scoped
builder.Services.AddScoped<IAuthService, AuthService>();         // Scoped
builder.Services.AddHostedService<DbInitializerHostedService>(); // Singleton, runs at startup
```

- **Scoped** means one instance per HTTP request. `AppDbContext` must be scoped because it is not thread-safe and tracks changes for a single unit of work.
- Controllers receive services in two ways, both resolved by DI:
  - **Constructor injection:** `public BookingController(IBookingService svc) => _svc = svc;`
  - **Action injection:** `[FromServices] IAuthService auth` (used in `AuthController`)
- Coding against interfaces (`IBookingService`) lets you swap in a mock (Moq) during tests.

`DbInitializerHostedService` is a singleton, so it cannot receive the scoped `AppDbContext` directly. That is why it does `_sp.CreateScope()` and resolves the DbContext from that scope.

### 4.5 Entity Framework Core concepts

| Concept | Where | Meaning |
|---|---|---|
| `DbContext` | `AppDbContext` | A session with the database. It holds `DbSet<T>` properties (one per table) and tracks changes. |
| `DbSet<T> Users => Set<T>()` | `AppDbContext` | Expression-bodied property: every access returns the context's set for `T`. |
| `OnModelCreating` (Fluent API) | `AppDbContext` | Maps classes to the **existing** table and column names (`ToTable("Service")`, `HasColumnName("office_id")`), keys, and relationships. It is needed because the SQL schema uses snake_case while C# uses PascalCase. |
| `HasKey(x => new { x.UserId, x.RoleId })` | `UserRole` | Composite primary key |
| `HasOne/WithMany/HasForeignKey` | `UserRole`, `Employee`, `Service`↔`ServiceDetail` | Relationship configuration |
| `Include(...).ThenInclude(...)` | `AuthService`, `ServicesService` | **Eager loading**: loads related rows in the same query (user → roles → role) |
| `AsNoTracking()` | all `GetAll`/`GetById` | Read-only queries. EF does not keep a change-tracking snapshot, so they are faster and use less memory. Do not use it when you plan to modify and save the entity. |
| Change tracking + `SaveChangesAsync` | Update/Delete methods | Entities loaded without `AsNoTracking` are tracked. Changing their properties and calling `SaveChangesAsync` produces `UPDATE` statements automatically. |
| `Select(s => MapToModel(s))` | services | Projection to a DTO. A **static** local method is allowed in the final `Select`. EF evaluates it on the client after reading the columns it needs. |
| Navigation-based insert | `AuthService.RegisterAsync` | `user.Roles.Add(new UserRole { User = user, Role = role })`. Since `user.Id` is still `0` before saving (the DB generates it), linking through **navigation properties** lets EF fill in `UserId` after the `INSERT INTO Users`. |
| `ExecuteSqlRawAsync` | `DatabaseBootstrapper` | Runs raw SQL (used to execute `schema.sql`) |
| `BeginTransactionAsync` / `CommitAsync` | `DatabaseBootstrapper` | All schema statements succeed together or none do |

**No EF migrations are used.** The schema comes from a hand-written SQL file instead (see [section 6](#6-database-schemasql-and-startup-bootstrapping)).

### 4.6 `record` types for DTOs

```csharp
public record LoginRequest(string Email, string Password);
```

A `record` declared with a parameter list (a "positional record") is an immutable class with a constructor, properties, value-based equality and `ToString()`, all generated by the compiler. That makes it a good fit for DTOs (Data Transfer Objects), which only carry data across the API boundary.

**Models vs DTOs:**

- `booking.core/Models/*` are **entities**: mutable classes that EF maps to tables.
- `booking.core/DTOs/*` are what the API receives and returns. This keeps the database shape separate from the public contract (for example, `PasswordHash` is never returned).

### 4.7 Nullable reference types (`?`, `!`, `default!`)

All projects enable `<Nullable>enable</Nullable>`.

- `string?` means the value may be null. `string` means it should never be null.
- `= default!;` (e.g. `public string RoleId { get; set; } = default!;`) tells the compiler "trust me, EF will set this," which silences the warning.
- `user.PasswordHash!` is the null-forgiving operator. It asserts the value is not null at that point.

### 4.8 `ImplicitUsings`

With `<ImplicitUsings>enable</ImplicitUsings>`, common namespaces (`System`, `System.Linq`, `System.Threading.Tasks`, …) are imported automatically. That is why `Task` and `CancellationToken` work without `using` lines.

### 4.9 Minimal hosting model (`Program.cs` without `Main`)

`Program.cs` uses **top-level statements**: no `class Program`, no `static void Main`. `WebApplication.CreateBuilder(args)` loads configuration (`appsettings.json`, `appsettings.{Environment}.json`, environment variables, user secrets), and `builder.Build()` creates the app.

### 4.10 Configuration (`IConfiguration`)

`appsettings.json` values are read with:

```csharp
builder.Configuration.GetConnectionString("DefaultConnection")  // "Data Source=app.db"
builder.Configuration.GetSection("Jwt")["Key"]
```

`AuthService` receives `IConfiguration` through DI to read the same `Jwt` section when it signs tokens. The settings layer on top of each other: environment variables or user secrets (`UserSecretsId` is set in `booking.api.csproj`) override `appsettings.json`.

### 4.11 Hosted services (`IHostedService`)

`DbInitializerHostedService` implements `IHostedService`. The host calls `StartAsync` **before the app starts accepting HTTP requests**, so the database is guaranteed to exist and be seeded before the first request. `StopAsync` does nothing here.

### 4.12 Attribute routing and model binding

| Attribute | Meaning |
|---|---|
| `[ApiController]` | Turns on API behaviors: automatic 400 responses on invalid model state, and `[FromBody]` inferred for complex types |
| `[Route("api/[controller]")]` | `[controller]` is replaced by the class name without `Controller` (e.g. `BookingController` → `api/Booking`) |
| `[HttpGet("{id:int}")]` | Route parameter with a **route constraint** (`:int`), so `/api/Booking/abc` does not match |
| `[FromBody]` | Read the JSON body |
| `[FromQuery]` | Read from the query string (`?serviceId=1`) |
| `[FromServices]` | Resolve from DI |
| `CreatedAtAction(nameof(GetById), new { id }, body)` | Returns **201 Created** with a `Location` header that points to the new resource |

---

## 5. File-by-file reference

### 5.1 `booking.api`

| File | Purpose |
|---|---|
| `Program.cs` | Composition root: CORS, controllers, Swagger (with a JWT "Authorize" button), `AppDbContext` on SQLite, JWT bearer validation, authorization policies, FluentValidation, service registrations, the DB-initializer hosted service, and the middleware pipeline. |
| `Controllers/AuthController.cs` | `login` and `register`. Maps service codes to HTTP results (all login failures → `401`). |
| `Controllers/BookingController.cs` | CRUD for bookings, plus `GET api/Booking/Service?serviceId=` (bookings for a service, with employee and detail names) and `POST api/Booking/availability` (time slots). |
| `Controllers/OfficeController.cs` | CRUD for offices. |
| `Controllers/ServiceController.cs` | CRUD for services and their nested service details. **The file name is `ServiceController.cs` but the class is `ServicesController`, so the route is `api/Services`.** |
| `Utils/FluentValidation.cs` | `RegisterRequestValidator`: email must be present and valid; password needs 8+ characters with upper, lower and digit. Auto-validation runs it before the action, and invalid requests get a 400 automatically. |
| `appsettings.json` | Connection string, JWT issuer, audience and key, logging. |
| `appsettings.Development.json` | Logging overrides for the Development environment. |
| `Properties/launchSettings.json` | `dotnet run` profiles: `http` → `http://localhost:5170`, `https` → `https://localhost:7251`. Swagger opens on launch. |
| `booking.api.http` | VS/VS Code REST-client scratch file. It still points at the template `/weatherforecast` endpoint. |

#### Middleware order in `Program.cs` (order matters)

```csharp
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAngularApp");   // must come before auth so preflight (OPTIONS) requests succeed
app.UseHttpsRedirection();
app.UseAuthentication();          // reads the JWT and builds HttpContext.User
app.UseAuthorization();           // checks policies against HttpContext.User
app.MapControllers();
```

The CORS policy `AllowAngularApp` allows only `http://localhost:4200` (the Angular dev server), with credentials.

### 5.2 `booking.core`

| File | Purpose |
|---|---|
| `Constants/Codes.cs` | String result codes returned in every JSON response (`OK`, `ERR_NOT_FOUND`, `ERR_AUTH_*`). The frontend can switch on these. |
| `Constants/Permissions.cs` | Permission names (`Booking.Read`, `Service.Create`, …) and the claim type `"permission"` used in JWTs and policies. Nested static classes give readable access such as `Permissions.Bookings.Read`. |
| `Models/User.cs` | `User`, `UserRole` (join table), `UserClaim` |
| `Models/Role.cs` | `Role`, `RoleClaim` |
| `Models/Employee.cs` | Employee linked 1:1 to a `User` (`user_id` is unique) |
| `Models/Office.cs` | Office (name, image) |
| `Models/Service.cs` | A bookable service in an office (e.g. "Room Booking") with booking rules |
| `Models/ServiceDetail.cs` | A concrete bookable item of a service (e.g. "Small Room") |
| `Models/Booking.cs` | A reservation: service detail + employee + start/end |
| `DTOs/Auth.cs` | `LoginRequest`, `RegisterRequest`, `AuthResponse` |
| `DTOs/Booking.cs` | `BookingModel`, `BookingUpsertDto`, `Availability` (request), `AvailabilityTimes` (response slot), `BookingRow` (flattened view) |
| `DTOs/Office.cs`, `DTOs/Service.cs`, `DTOs/Employee.cs` | Read models and upsert DTOs ("upsert" = the same DTO is used for create and update) |

### 5.3 `booking.infrastructure`

| File | Purpose |
|---|---|
| `Persistence/AppDbContext.cs` | EF Core context and Fluent API mapping to the tables in `schema.sql` |
| `Persistence/Sql/schema.sql` | DDL that creates every table (see [section 6](#6-database-schemasql-and-startup-bootstrapping)) |
| `Persistence/DatabaseBootstrapper.cs` | Creates the schema if missing, then seeds demo data. **This is the one that runs.** |
| `Persistence/DbInitializerHostedService.cs` | Calls `DatabaseBootstrapper.InitAsync` at startup |
| `Persistence/DbInitializer.cs` | Older alternative that uses `EnsureCreatedAsync()` and seeds only the admin user. **Nothing calls it.** It is dead code. |
| `Services/AuthService.cs` | Login, register, JWT creation |
| `Services/BookingService.cs` | Booking CRUD, bookings by service, availability slots |
| `Services/OfficeService.cs` | Office CRUD |
| `Services/ServiceService.cs` | Service CRUD. On update it **replaces** all service details (deletes the old ones and inserts new ones). |

#### `AuthService.LoginAsync`, step by step

1. Find the user by email, case-insensitively (`ToLower()` on both sides), eager-loading `Roles → Role`.
2. If there is no user → `ERR_AUTH_USER_NOT_FOUND`. If there is no hash, or `PasswordHasher.VerifyHashedPassword` fails → `ERR_AUTH_INVALID_CREDENTIALS`.
3. Collect role names.
4. Collect permissions from **both** `UserClaims` (granted directly to the user) and `RoleClaims` (granted through the user's roles), filtered by `ClaimType == "permission"`, then de-duplicated.
5. Look up the linked `Employee` id (if any).
6. Build a JWT (`CreateJwt`) and return an `AuthResponse`.

#### `AuthService.RegisterAsync`

Validates email and password strength (this repeats the FluentValidation rules, as a second check at the service level), rejects duplicate emails, hashes the password, ensures the `r_user` role exists, links the user to it, saves, and returns a token right away (so the user is logged in as soon as they register). `EmailConfirmed` is set to `true` immediately; there is no confirmation email flow.

#### `CreateJwt`

- Algorithm: **HMAC-SHA256** with the symmetric key from `Jwt:Key`.
- Claims: `sub` and `NameIdentifier` (user id), `Email`, `Name`, one `Role` claim per role, one `permission` claim per permission, and `employee_id` when the user is linked to an employee.
- Expires **2 hours** after issue (UTC).

#### `PasswordHasher<User>`

From ASP.NET Core Identity. It produces a salted **PBKDF2** hash with a version marker. `VerifyHashedPassword` returns `Failed`, `Success`, or `SuccessRehashNeeded`. The last one means the password is correct but was hashed with older parameters; this code treats it as success but does not re-hash.

#### `BookingService.GetAvailability`

Input: `{ id: serviceDetailId, date: "2024-10-18", frequency: "30" }`.

1. Build slots from **08:00 to 15:00** (hard-coded), stepping by `frequency` minutes.
2. Load bookings for that service detail whose start falls on that date.
3. A slot is unavailable if some booking satisfies `start <= slot < end`.
4. Return `[{ time: "08:00", isAvailable: true }, …]`.

---

## 6. Database: `schema.sql` and startup bootstrapping

### 6.1 What `schema.sql` is

`booking.infrastructure/Persistence/Sql/schema.sql` is a plain **SQLite DDL script** (Data Definition Language: `CREATE TABLE`, `CREATE INDEX`). It is the **single source of truth for the database structure**. The project does **not** use EF Core migrations or `EnsureCreated()` to create tables. Instead:

- `schema.sql` defines the tables, column names and types, primary keys, foreign keys, defaults and indexes.
- `AppDbContext.OnModelCreating` then **maps** the C# classes onto those existing tables and columns.

If you change a table, you must change **both** `schema.sql` and the matching entity/mapping, then delete `app.db` so the database is recreated (see 6.3).

### 6.2 Tables

The design follows the shape of ASP.NET Core Identity tables (Users, Roles, UserRoles, UserClaims, RoleClaims, UserLogins, UserTokens), plus the booking domain tables.

| Table | Key | Purpose / notes |
|---|---|---|
| `Users` | `Id INTEGER PRIMARY KEY` (auto-generated rowid) | Login accounts. Index `IX_Users_Email`. Booleans are stored as `INTEGER` 0/1 (SQLite has no bool type). |
| `Roles` | `Id TEXT` (e.g. `r_admin`, `r_user`) | Role names |
| `UserRoles` | composite (`UserId`, `RoleId`) | Many-to-many Users ↔ Roles, `ON DELETE CASCADE` |
| `UserClaims` | `Id AUTOINCREMENT` | Permissions granted directly to a user |
| `RoleClaims` | `Id AUTOINCREMENT` | Permissions granted to a role |
| `UserLogins`, `UserTokens` | composite | External login / token storage. **Not mapped in EF and not used yet.** |
| `Employees` | `ID` | Person data. `user_id` → `Users`, **unique** index `UQ_Employees_User` (one employee per user) |
| `Office` | `ID` | Physical or remote office |
| `Service` | `ID` | Bookable service in an office. `office_id` → `Office`. Rules: `quantity`, `max_bookings_week`, `booking_per_time`, `min_max` (e.g. `"15-45"` minutes) |
| `Service_Details` | `ID` | Concrete bookable items. `service_id` → `Service` |
| `Booking` | `ID` | Reservation. `service_detail_id` → `Service_Details`, `employee_id` → `Employees` |

```
Users 1──* UserRoles *──1 Roles 1──* RoleClaims
  │ 1
  ├──* UserClaims
  └──1 Employees 1──* Booking *──1 Service_Details *──1 Service *──1 Office
```

SQLite details worth knowing:

- `INTEGER PRIMARY KEY` aliases SQLite's internal `rowid`, so ids are generated automatically when you insert without one.
- Dates are stored as **`TEXT`** (ISO-8601 strings). The EF Core SQLite provider converts `DateTime` to and from that format for you.
- `DEFAULT (datetime('now'))` fills `created_at` / `CreatedAt` with the current UTC time when no value is given.
- Column names are case-insensitive in SQLite, so `Booking.ID` maps to C# `Id` without `HasColumnName`.

### 6.3 How the database is created at startup

```
App starts
  └─ DbInitializerHostedService.StartAsync()
       └─ DatabaseBootstrapper.InitAsync(db, "<api content root>/../booking.infrastructure")
            1. SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'
            2. If Users does not exist:
                 - read Persistence/Sql/schema.sql
                 - split it on ";\n" into statements
                 - run every statement inside ONE transaction
            3. SeedAsync()   ← idempotent: safe to run every startup
```

- `sqlite_master` is SQLite's internal catalog table. Querying it is how the code checks whether the schema already exists.
- The SQLite file is created automatically on first connection (`Data Source=app.db`, relative to the API's working directory).
- **To reset the database:** stop the app, delete `src/booking.api/app.db`, and start again.

**Seed data** (each block is added only if missing, which is what *idempotent* means here):

| Data | Values |
|---|---|
| Users 1–4 | `admin@demo.com`, `sarah.johnson@company.com`, `michael.chen@company.com`, `emma.rodriguez@company.com`. **Password for all: `P@ssw0rd!`** |
| Role | `r_admin` ("Admin") assigned to user 1 |
| Role claims | `r_admin` gets `Booking.Read` and `Booking.Create` |
| Offices | 1 "Main Office", 2 "Remote Office" |
| Employees | 101–104 linked to users 1–4 |
| Services | 1 "Room Booking" (details: Small Room, Large Room), 2 "Equipment Rental" (details: Projector, Laptop) |
| Booking | One booking for Sarah on Small Room, 2024-10-18 10:00–10:45 |

**Path detail:** the bootstrapper finds `schema.sql` through `ContentRootPath/../booking.infrastructure/...`, i.e. **the source folder**, not the build output. The `.csproj` copies `schema.sql` to the output directory (`CopyToOutputDirectory`), but that copy is never used. This works with `dotnet run` from the source tree, but it **breaks after `dotnet publish`** or in Docker, where that folder does not exist. A more robust option is `Path.Combine(AppContext.BaseDirectory, "Persistence", "Sql", "schema.sql")`.

---

## 7. Authentication and authorization

### 7.1 Authentication (who you are), JWT Bearer

1. The client calls `POST /api/auth/login` and receives `data.token`.
2. The client sends `Authorization: Bearer <token>` on later requests.
3. `UseAuthentication()` validates the token: **issuer** (`booking`), **audience** (`booking_client`), **signature** (`Jwt:Key`), and **lifetime** (not expired).
4. If valid, `HttpContext.User` is populated with the token's claims.

`Program.cs` refuses to start if `Jwt:Key` is missing or shorter than **32 bytes**, because HS256 needs a key of at least 256 bits.

In Swagger, click **Authorize** and paste the token to call protected endpoints.

### 7.2 Authorization (what you may do), claim-based policies

`Program.cs` defines one policy per permission:

```csharp
opt.AddPolicy("Booking.Read", p => p.RequireClaim(Permissions.ClaimType, Permissions.Bookings.Read));
```

A policy succeeds when the user's token contains a claim `permission = Booking.Read`. Permissions reach the token through `RoleClaims` (via roles) or `UserClaims` (direct grants).

To protect an endpoint:

```csharp
[Authorize(Policy = "Booking.Read")]
[HttpGet]
public async Task<IActionResult> GetAll(CancellationToken ct) { ... }
```

> **Current state:** the policies are defined and the controllers import `Microsoft.AspNetCore.Authorization`, but **no controller or action has an `[Authorize]` attribute**. Every endpoint is currently public. See [section 10](#10-known-issues-and-things-to-watch).

---

## 8. API endpoints

Base URL (dev): `http://localhost:5170`. Every response has the shape `{ "code": "...", "data": ... }` or `{ "code": "...", "message": "..." }`.

### Auth: `api/Auth`

| Method | Route | Body | Success | Errors |
|---|---|---|---|---|
| POST | `/api/Auth/login` | `{ email, password }` | 200 `AuthResponse` | 401 |
| POST | `/api/Auth/register` | `{ email, password, userName? }` | 200 `AuthResponse` | 400 |

### Bookings: `api/Booking`

| Method | Route | Notes |
|---|---|---|
| GET | `/api/Booking` | All bookings |
| GET | `/api/Booking/{id}` | 404 if missing |
| GET | `/api/Booking/Service?serviceId=1` | `BookingRow[]` with employee and detail names. 400 if `serviceId` is missing, 404 if empty |
| POST | `/api/Booking` | `BookingUpsertDto` → 201 + `Location` header |
| PUT | `/api/Booking/{id}` | 404 if missing |
| DELETE | `/api/Booking/{id}` | 404 if missing |
| POST | `/api/Booking/availability` | `{ id, date, frequency }` → `AvailabilityTimes[]` |

### Offices: `api/Office`

Standard CRUD: `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`.

### Services: `api/Services`

Standard CRUD (same as Offices). The body includes `serviceDetails[]`, which are created along with the service and **fully replaced** on update.

---

## 9. Running the backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/booking.api/booking.api.csproj --launch-profile http
# Swagger: http://localhost:5170/swagger
```

Run from the source tree (see the path note in 6.3). Log in with `admin@demo.com` / `P@ssw0rd!`.

Tests:

```bash
dotnet test
```

Overriding secrets without editing `appsettings.json`:

```bash
dotnet user-secrets set "Jwt:Key" "<at least 32 random bytes>" --project src/booking.api
# or an environment variable (a double underscore stands for ':')
Jwt__Key=<secret> dotnet run --project src/booking.api
```

---

### Running in Docker / Kubernetes

- Container files live in `backend/deploy/` (see [`deploy/README.md`](deploy/README.md)).
- `GET /healthz` returns `Healthy` (anonymous) and is used by the Docker and Kubernetes probes.
- Allowed CORS origins come from `Cors:AllowedOrigins` (default `http://localhost:4200` in
  `appsettings.json`; the cluster sets `Cors__AllowedOrigins__0`).
- In the container the database is `/data/app.db` and `schema.sql` is copied to
  `/app/booking.infrastructure/Persistence/Sql/` to match the path `DbInitializerHostedService` expects.

## 10. Known issues and things to watch

These are observations from reading the code, as a to-do list. Nothing here has been changed.

| # | Area | Issue |
|---|---|---|
| 1 | Security | **No `[Authorize]` on any controller.** All CRUD endpoints are public even though policies exist. |
| 2 | Security | The JWT signing key is committed in `appsettings.json`. Move it to user-secrets or environment variables and rotate it. |
| 3 | Security | Login returns different codes for "user not found" and "wrong password" (`ERR_AUTH_USER_NOT_FOUND` vs `ERR_AUTH_INVALID_CREDENTIALS`), so callers can find out which emails are registered even though the message is the same. |
| 4 | Robustness | `GetAvailability`: `int.Parse(dto.frequency)` / `DateTime.Parse(dto.date)` throw on bad input (500). `frequency = "0"` or a negative value creates an **infinite loop**, since `time` never advances. Validate that `frequency > 0`. |
| 5 | Data | `schema.sql` foreign keys on `Service`, `Service_Details` and `Booking` have no `ON DELETE CASCADE`. Deleting an office, service or service detail that still has children will probably fail with a SQLite `FOREIGN KEY constraint failed` error (500). `ServicesService.UpdateAsync` also deletes and re-creates details, which fails the same way if a detail already has bookings. |
| 6 | Data | `BookingService.CreateAsync` performs no checks: no overlap check, no `MaxBookingsPerWeek` / `BookingPerTime` / `MinMax` rules, and no `start < end` check. |
| 7 | Deployment | `schema.sql` is located through the source folder, which breaks after publish (see 6.3). |
| 8 | Bootstrapper | `await using (var conn = db.Database.GetDbConnection())` disposes the connection that **EF owns**. Normally you should open or close it but not dispose it, and let the DbContext manage it. |
| 9 | Bootstrapper | `SplitSqlStatements` splits on `";\n"`, so a `;` inside a string literal, or two statements on one line, would break it. That is fine for the current file but fragile. |
| 10 | Performance | `u.Email.ToLower() == email.ToLower()` cannot use `IX_Users_Email`. Consider storing a normalized (lower-case) email column or using `COLLATE NOCASE`. |
| 11 | Cleanup | `DbInitializer.cs` is unused. `Class1.cs` files are empty. The `AutoMapper` package is referenced but unused. `booking.api.http` points to `/weatherforecast`. |
| 12 | Packages | The FluentValidation packages are on mixed major versions (`AspNetCore` 11.3.1 vs `DependencyInjectionExtensions` 12.0.0), and `FluentValidation.AspNetCore` auto-validation is deprecated upstream. `Microsoft.AspNetCore.Identity` 2.3.1 is an old ASP.NET Core 2.x-era package used only for `PasswordHasher<T>`. |
| 13 | Tests | `test/booking.api.test` contains only an empty placeholder test. |
| 14 | Consistency | `GetByServiceId` returns `ERR_NOT_FOUND` for a **missing parameter** (400). A distinct code such as `ERR_VALIDATION` would be clearer. |
