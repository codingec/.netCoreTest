# Backend Improvement Proposal — booking-app

| | |
|---|---|
| **Scope** | `backend/` (booking.api, booking.core, booking.infrastructure, booking.api.test) |
| **Baseline** | `master` @ `76724e8` + uncommitted `AuthService.cs` fix |
| **Date** | 2026-11-02 |
| **Audience** | Backend engineers / tech lead |
| **Companion doc** | [`DOC_BACKEND.md`](DOC_BACKEND.md) explains *how the code works today*; this document proposes *what to change and in what order* |

---

## 1. Executive summary

The backend has a sensible shape: a three-project split, EF Core, JWT, permission-based policies, async all the way down with `CancellationToken`, and a working Swagger. But it is still **hackathon-grade**. It must not be exposed beyond localhost in its current state.

The three things that matter most:

1. **There is no authorization.** No controller has `[Authorize]`. Every endpoint, including delete operations, is anonymous. The JWT secret is committed to git. The client chooses `employeeId` when booking, so anyone can book on behalf of anyone.
2. **The booking domain is not enforced.** There is no overlap check, no `start < end` check, and none of the configured rules (`MaxBookingsPerWeek`, `BookingPerTime`, `MinMax`, `Capacity`, `Quantity`) are applied. The availability algorithm returns wrong answers for partially overlapping bookings. It can also be driven into an infinite loop with `frequency=0`.
3. **There are no tests, no migrations, and an aging platform.** **.NET 8 goes out of support on 2026-11-10, eight days from now.** The schema is a hand-written `schema.sql` that only runs once, so there is no way to evolve it. The test project has one empty test and doesn't reference any source project.

**Recommendation:** run a short **stabilization phase (≈1 sprint)** that covers security, correctness, and a test harness. Then do a **platform/architecture phase** that covers .NET 10, migrations, and a consistent error model. After that, invest in the booking domain itself. Section 5 has the phased roadmap.

---

## 2. Method

- Read every source file in `src/` and `test/`, plus `schema.sql`, the configuration, and the `.csproj` files.
- Cross-checked against the frontend (`frontend/src`) to see how the API is consumed. Notable: the frontend **sends no `Authorization` header**, so turning on auth needs a coordinated frontend change.
- Severity scale:
  - **P0**: exploitable, or loses or corrupts data. Fix before any shared deployment.
  - **P1**: wrong behavior, or a 500 on normal input.
  - **P2**: maintainability, performance, operability.
  - **P3**: hygiene.

---

## 3. Findings

### 3.1 Summary table

| ID | Sev | Area | Finding | Location |
|---|---|---|---|---|
| S1 | P0 | Security | No `[Authorize]` anywhere; the 12 defined policies are never used | `Controllers/*.cs` |
| S2 | P0 | Security | JWT signing key committed to git in `appsettings.json` | `appsettings.json:8` |
| S3 | P0 | Security | Booking `employeeId` comes from the request body, so anyone can book or edit for any employee (IDOR) | `BookingService.cs:73-103` |
| S4 | P0 | Security / DoS | `frequency <= 0` makes `GetAvailability` loop forever and grow a list until the process runs out of memory | `BookingService.cs:137` |
| S5 | P1 | Security | User enumeration: the login response `code` tells "user not found" apart from "wrong password" | `AuthController.cs:20-21` |
| S6 | P1 | Security | No rate limiting or lockout on `/api/auth/login` and `/register` | `Program.cs` |
| S7 | P1 | Security | Seed users with a well-known password (`P@ssw0rd!`) are created in **every** environment | `DatabaseBootstrapper.cs:94` |
| S8 | P2 | Security | Swagger enabled in all environments; `AllowedHosts: *`; CORS origin hard-coded | `Program.cs:101-103` |
| D1 | P0 | Domain | No double-booking protection: no overlap check, no transaction, no DB constraint | `BookingService.cs:73` |
| D2 | P1 | Domain | Availability only tests whether the *slot start* falls inside a booking, so a booking at 10:15 leaves the 10:00 slot shown as free | `BookingService.cs:143` |
| D3 | P1 | Domain | Service rules (`MaxBookingsPerWeek`, `BookingPerTime`, `MinMax`, `Capacity`, `Quantity`) are stored but never enforced | `Service.cs`, `BookingService.cs` |
| D4 | P1 | Domain | No validation of `start < end`, of past dates, or of the booking window; working hours `08:00–15:00` are hard-coded | `BookingService.cs:117-118` |
| D5 | P1 | Domain | Registered users get role `r_user` with **zero** permissions and no `Employee` row, so they can never book once auth is on | `AuthService.cs:100-127` |
| D6 | P1 | Domain | Seeded admin only gets `Booking.Read` and `Booking.Create`, so turning on policies locks the admin out of everything else | `DatabaseBootstrapper.cs:110-120` |
| D7 | P1 | Data | `ServicesService.UpdateAsync` deletes and recreates every `ServiceDetail`. This changes IDs, and it fails with an FK error (500) once a detail has bookings | `ServiceService.cs:92-108` |
| D8 | P1 | Data | Deleting an `Office` or `Service` that has children fails with an FK error (500); there is no cascade in the DB and no domain rule (soft delete? block?) | `schema.sql:91,102,112` |
| D9 | P1 | Data | Client supplies primary keys (`Office.Id`, `Booking.Id`) and `createdAt` on create; sending an existing ID gives a 500 | `OfficeService.cs:48`, `BookingService.cs:77,82` |
| D10 | P1 | Data | Time handling: `DateTime` with mixed `Kind`, stored as TEXT. `DateTime.Parse("…Z")` in the seed converts to the **server's local** time | `DatabaseBootstrapper.cs:263`, `BookingService.cs:121` |
| D11 | P2 | Data | `Office.image` is `NOT NULL` in SQL but nullable in the DTO and model, so a create without an image gives a 500 | `schema.sql:77`, `Office.cs` |
| D12 | P2 | Data | `Users.Email` has no `UNIQUE` constraint. The duplicate check is check-then-insert, which races | `schema.sql:12`, `AuthService.cs:88` |
| D13 | P2 | Data | The demo booking is not seeded on a fresh DB. `SeedAsync` looks up the employee and service detail *before* `SaveChangesAsync`, so both are `null` and it only appears on the second start (found by the new tests) | `DatabaseBootstrapper.cs:252-268` |
| A1 | P1 | API | No global exception handling. Every DB or parse error leaks as a raw 500 | `Program.cs` |
| A2 | P2 | API | Custom `{code, data/message}` envelope built ad hoc in every action; no RFC 9457 `ProblemDetails`; status codes are inconsistent | `Controllers/*.cs` |
| A3 | P2 | API | Empty result returns 404 (`GetByServiceId`); a missing parameter returns 400 with `ERR_NOT_FOUND`; availability is a `POST` that only reads | `BookingController.cs:36-42,74` |
| A4 | P2 | API | No pagination or filtering on list endpoints | all `GetAll` |
| A5 | P2 | API | Validation exists only for `RegisterRequest`, and the same rules are duplicated in `AuthService` | `Utils/FluentValidation.cs` |
| P1 | P1 | Platform | **.NET 8 end of support: 2026-11-10.** Upgrade to .NET 10 LTS | all `.csproj` |
| P2 | P1 | Platform | No migrations. `schema.sql` runs only if `Users` is missing, so schema changes cannot be deployed | `DatabaseBootstrapper.cs` |
| P3 | P1 | Platform | `schema.sql` is resolved via `ContentRoot/../booking.infrastructure`, which breaks on `dotnet publish` and in containers | `DbInitializerHostedService.cs:22` |
| P4 | P2 | Platform | Package hygiene: mixed FluentValidation majors, deprecated `FluentValidation.AspNetCore`, legacy `Microsoft.AspNetCore.Identity` 2.3.1, unused AutoMapper, JwtBearer pinned to 8.0.0 (unpatched) | `*.csproj` |
| T1 | P1 | Quality | ~~No tests~~. **Partly addressed:** there are now test projects for core, infrastructure and api. Known issues are kept as skipped tests named after these IDs | `test/` |
| T2 | P2 | Quality | No CI, no analyzers, warnings not treated as errors, no `.editorconfig`, no Central Package Management | repo root |
| O1 | P2 | Ops | No structured logging, health checks, correlation IDs or metrics | `Program.cs` |
| C1 | P3 | Cleanup | Dead code: `DbInitializer.cs`, both `Class1.cs`, the `weatherforecast` `.http` file, unused `using`s, Spanish/English comment mix, a validator in the global namespace | various |

### 3.2 Details on the non-obvious items

#### S3: Booking ownership comes from the client
`BookingUpsertDto.employeeId` is trusted as-is on create and update. Even after `[Authorize]` is added, any authenticated user can create, move or delete another employee's booking. The employee must come from the token's `employee_id` claim. A separate admin path can book on behalf of others, guarded by its own permission. Update and delete must check ownership as well as the permission.

#### S4 / D2: The availability algorithm
```csharp
for (var time = startTime; time < endTime; time = time.AddMinutes(int.Parse(dto.frequency)))
    if (b.StartDatetime <= time && b.EndDatetime > time) // only tests the slot *start*
```
- `frequency = "0"` or a negative value means the loop never ends. Each iteration allocates, so the result is unbounded memory growth. A single anonymous request can take the process down.
- A non-numeric `frequency` or `date` throws `FormatException`, which becomes a 500.
- Overlap is wrong. The correct test for half-open intervals is `b.Start < slotEnd && b.End > slotStart`.
- It ignores `Capacity` and `BookingPerTime`, so a resource that allows N concurrent bookings shows as taken after one.
- It only loads bookings whose *start* is on that date, so a booking spanning midnight is missed.

#### D1: Double booking needs a real guarantee
An application-level overlap check alone is a classic check-then-act race. Options, in order of strength:
1. A check plus insert inside a **serializable transaction**. SQLite serializes writers anyway, so this works today on a single instance.
2. On PostgreSQL (see §4.5), an **exclusion constraint**:
   `EXCLUDE USING gist (service_detail_id WITH =, tstzrange(start_at, end_at) WITH &&)`.
   This is airtight for capacity = 1. Capacity > 1 still needs the transactional check, or a slot table with a counter and optimistic concurrency.

#### D7: Updating a service destroys its details
Replacing the whole child collection gives new `ServiceDetail` IDs on every edit. Every booking points at `service_detail_id`, so this is either an FK failure (with `PRAGMA foreign_keys=ON`, which EF's SQLite provider enables) or silent orphaning. The DTO already carries `ServiceDetailUpsertDto.id`, so diff by ID instead: update matches, add new ones, and remove missing ones only if they have no bookings (or soft delete them).

#### D10: Time
`DateTime.Parse("2024-10-18T10:00:00Z")` returns a **local** `DateTime`. The seed data therefore depends on the timezone of whichever machine first ran the app. Incoming request timestamps are deserialized with whatever offset the client sent and stored without one. Proposal:
- Store UTC only and use `DateTimeOffset` at the API boundary.
- Give each `Office` a `TimeZoneId` so working hours (currently hard-coded `08:00–15:00`) are interpreted in local time.
- Use `DateOnly` / `TimeOnly` for date and slot parameters.
- Inject `TimeProvider` so that "now" can be tested.

#### D5 / D6: The permission model is wired but not populated
Enabling `[Authorize(Policy = …)]` today would lock everyone out. The admin lacks 10 of the 12 permissions, and `r_user` has none. Permissions also live **inside the JWT** for 2 hours, so revoking a permission takes effect only when the token expires. That is acceptable at this scale, but it should be a deliberate decision (see §6).

#### P3: Schema bootstrapping breaks on publish
`schema.sql` is copied to the output directory (`CopyToOutputDirectory`), but the hosted service reads it from `ContentRoot/../booking.infrastructure/Persistence/Sql/`, which only exists in a source checkout. Migrations make this moot (see §4.4).

#### Pending local change: commit it
The uncommitted change in `AuthService.RegisterAsync` is a **correct bug fix**. The old code added `UserRole { UserId = user.Id }` while `user.Id` was still `0`. Linking through navigations lets EF fill in the key on save. Commit it with a regression test (§4.6).

---

## 4. Proposed design changes

### 4.1 Security baseline (P0)
```csharp
// Program.cs: secure by default, opt out explicitly
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Policies generated from the Permissions constants instead of 12 hand-written lines
foreach (var p in Permissions.All)
    authz.AddPolicy(p, b => b.RequireClaim(Permissions.ClaimType, p));
```
- Add `[AllowAnonymous]` only on `AuthController`.
- Add `[Authorize(Policy = Permissions.Bookings.Create)]` (and so on) per action.
- Move `Jwt:Key` to user-secrets (dev) or environment / Key Vault (prod), and **rotate it**: the current key is in git history and must be treated as compromised.
- Bind JWT settings with `IOptions<JwtOptions>` plus `ValidateOnStart()`. Build the signing key once and register it as a singleton; don't rebuild it on every login (`AuthService.CreateJwt`).
- Set `ClockSkew` explicitly (the default is 5 minutes).
- Rate limiting (`AddRateLimiter`): a fixed window per IP on `/api/auth/*`.
- Return one code (`ERR_AUTH_INVALID_CREDENTIALS`) for both failure modes. Verify against a dummy hash when the user is missing, to equalize timing.
- Handle `PasswordVerificationResult.SuccessRehashNeeded`: re-hash and save.
- Seed demo users **only in Development**; seed roles and permissions through the migration path in every environment.
- Swagger only in Development. CORS origins and `AllowedHosts` come from configuration.
- **Frontend dependency:** add an Angular `HttpInterceptor` that attaches `Authorization: Bearer …`, and handle 401 by redirecting to login. Ship it together with the backend change.

### 4.2 Error model and API contract (P1/P2)
Replace `(bool ok, T data, string code, string? message)` tuples with a small `Result<T>` / `Error` type. Map it to HTTP in one place:

```csharp
public sealed record Error(string Code, string Message, ErrorKind Kind); // NotFound, Validation, Conflict, Forbidden
public readonly record struct Result<T>(T? Value, Error? Error) { public bool IsSuccess => Error is null; }

// One extension that turns Result<T> into an IActionResult / IResult using ProblemDetails
return result.ToActionResult(this);
```
- `AddProblemDetails()` + `UseExceptionHandler()` + an `IExceptionHandler` that maps `DbUpdateException` (unique or FK violation) to 409, `ValidationException` to 400, and everything else to 500 with a `traceId`. **No raw stack traces.**
- Keep the existing `Codes` as ProblemDetails `extensions["code"]` so the frontend's error-code handling still works.
- Validators for **every** request DTO (`LoginRequest`, `BookingUpsertDto`, `ServiceUpsertDto`, `OfficeUpsertDto`, `Availability`). Remove the duplicated password rules from `AuthService`. Move to `FluentValidation` 12 with an endpoint filter or an explicit `IValidator<T>` call, since `FluentValidation.AspNetCore` auto-validation is deprecated.
- Separate create and update DTOs. **Remove `id` and `createdAt` from inputs**; the server owns them.
- Route cleanup:

| Today | Proposed |
|---|---|
| `GET /api/booking/Service?serviceId=1` | `GET /api/services/{id}/bookings` |
| `POST /api/booking/availability` | `GET /api/service-details/{id}/availability?date=2026-11-03&slotMinutes=30` |
| `GET /api/booking` (all, unpaged) | `GET /api/bookings?from=&to=&employeeId=&page=&pageSize=` |
| `/api/Booking` vs `/api/Services` (singular/plural mix) | consistent plural, lowercase |

- Empty collections return `200 []`, not 404.
- Version the API (`/api/v1`) before the contract changes, so the frontend can migrate gradually.

### 4.3 Booking domain (P0/P1)
Move the rules out of the CRUD service into a dedicated `BookingPolicy` / domain service in `booking.core` that has **no EF dependency**, so it can be unit-tested directly:

| Rule | Source field | Check |
|---|---|---|
| Valid interval | — | `start < end`, `start >= now`, inside the office's working hours |
| Duration bounds | `Service.MinMax` → split into `MinDurationMinutes` / `MaxDurationMinutes` (ints, not `"15-45"`) | `min <= (end - start) <= max` |
| Concurrency per resource | `ServiceDetail.Capacity` / `Service.BookingPerTime` | overlapping bookings `< capacity` |
| Weekly quota | `Service.MaxBookingsPerWeek` | the employee's bookings in that ISO week `< max` |
| Ownership | JWT `employee_id` | the employee comes from the token; admin override needs its own permission |

- Run create and update inside a transaction (see D1). Return `409 Conflict` with code `ERR_BOOKING_CONFLICT` on overlap.
- Rewrite availability as a pure function `(slots, bookings, capacity) -> AvailabilitySlot[]` with correct half-open overlap. Clamp `slotMinutes` to e.g. 5–240.
- Fix registration: either create an `Employee` row as part of registration, or make booking require an employee profile explicitly. This is a product decision (§6).
- Add a booking `Status` (`Confirmed` / `Cancelled`) and cancel instead of hard-deleting. This keeps history and avoids the FK problems in D8.

### 4.4 Persistence (P1)
- **Adopt EF Core migrations.** Generate a baseline migration that matches the current `schema.sql`. For existing `app.db` files, insert the baseline row into `__EFMigrationsHistory` manually. Then delete `schema.sql`, `DatabaseBootstrapper`'s SQL splitter, `DbInitializer.cs`, and the path hack. Apply migrations through a release step (`dotnet ef migrations bundle`), not at app start in production.
- Schema fixes in the first real migration:
  - `UNIQUE` on a `NormalizedEmail` column, and look up users by it (this also fixes the non-sargable `ToLower()` in D12 / §3.1).
  - Index `Booking(service_detail_id, start_datetime)` and `Booking(employee_id, start_datetime)`.
  - `CHECK (end_datetime > start_datetime)`.
  - Explicit `ON DELETE` behavior per relationship: `RESTRICT` for Office→Service→Detail→Booking, plus soft delete (`IsActive` / `DeletedAt`) for catalog entities.
  - Align nullability between SQL, model and DTO (`Office.image`, `Employees.user_id`).
- Move the fluent mappings out of `OnModelCreating` into `IEntityTypeConfiguration<T>` classes (`ApplyConfigurationsFromAssembly`). Use a naming convention instead of per-property `HasColumnName`.
- Add `CreatedAt` / `UpdatedAt` through a `SaveChanges` interceptor using `TimeProvider`, not set in the services.
- Use projections (`Select` into DTO) instead of loading entities and mapping in memory. Use `AsSplitQuery` for `Service` + `ServiceDetails`.

### 4.5 Platform (P1)
- **.NET 8 → .NET 10 (LTS)** for all projects, with EF Core 10 and ASP.NET Core 10. Do this first in phase 2. It is mostly mechanical for a codebase this size.
- Add `Directory.Build.props` (`Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`) and `Directory.Packages.props` (Central Package Management).
- Packages:
  - Remove `AutoMapper` (unused; recent versions are commercially licensed).
  - Replace `Microsoft.AspNetCore.Identity` 2.3.1 with `Microsoft.Extensions.Identity.Core`; only `PasswordHasher<T>` is needed.
  - Align FluentValidation on 12.x.
  - Track the latest patch of JwtBearer.
- Swagger: .NET 9+ ships `Microsoft.AspNetCore.OpenApi` document generation. Consider dropping Swashbuckle and using Scalar or Swagger UI on top of the built-in document.
- **Database:** SQLite is fine for local development and a single-instance demo. If this becomes multi-user or production, move to **PostgreSQL**: real `timestamptz`, exclusion constraints for D1, and concurrent writers. Migrations (§4.4) make this switch cheap, so it doesn't need deciding now; just avoid writing more raw SQLite SQL.
- Containerize with a `Dockerfile` (chiseled `aspnet` image) and a `docker-compose` for API + DB.

### 4.6 Testing strategy (P1)
| Layer | Tooling | What to cover first |
|---|---|---|
| Unit | xUnit + FluentAssertions | `BookingPolicy` rules, the availability function (overlap edges, capacity, `slotMinutes` bounds), `Result` → HTTP mapping |
| Integration | `WebApplicationFactory<Program>` + **SQLite in-memory** (not EF InMemory, which ignores FKs, constraints and transactions); Testcontainers if moving to Postgres | auth flow (register → login → protected call), 401/403 per policy, double-booking conflict, service update keeps detail IDs, delete with children |
| Contract | snapshot of the OpenAPI document | catches accidental API breaks against the frontend |

- Fix the test project: reference `booking.api` and `booking.infrastructure`, and add `public partial class Program;`.
- First regression tests to write: the `RegisterAsync` UserRole fix, `frequency=0`, and partial-overlap availability.
- CI (GitHub Actions): `dotnet build -warnaserror`, `dotnet test` with coverage, `dotnet list package --vulnerable`, and a secret scan (gitleaks), on every PR.

### 4.7 Observability and operations (P2)
- Structured logging (Serilog or the built-in JSON console) with request logging and a correlation/trace ID that is also returned in ProblemDetails.
- `AddHealthChecks()` with a DB check at `/health/live` and `/health/ready`.
- OpenTelemetry traces and metrics (ASP.NET Core + EF Core instrumentation). This is cheap to add now.
- Audit log for booking create, update and cancel: who, when, and what changed.

### 4.8 Code organization (P2/P3)
The current split is reasonable, but business logic lives in `infrastructure` next to EF. Proposed, without over-engineering:

```
booking.core            → entities, value objects, domain rules (BookingPolicy, Availability), Result/Error, interfaces
booking.application     → (optional) use-case services, DTOs, validators
booking.infrastructure  → EF DbContext, configurations, migrations, JWT/token service, password hashing
booking.api             → endpoints, auth policies, ProblemDetails mapping, composition root
```
- Keep service-per-aggregate. Don't add MediatR, a repository-over-EF layer, or CQRS libraries; the domain is too small to justify them.
- Split `Program.cs` into extension methods (`AddBookingAuth()`, `AddBookingPersistence()`, …).
- Consider Minimal API endpoint groups for new endpoints; controllers are fine to keep.
- Cleanup: delete the dead files in C1, standardize comments in English, and give the validator a namespace.

---

## 5. Roadmap

Effort is in engineer-days for one developer who knows .NET.

### Phase 1 — Stabilize (≈ 1 sprint, ~6–8 d) *Blocker for any shared deployment*
| # | Item | Findings | Est. |
|---|---|---|---|
| 1 | Commit the `RegisterAsync` fix; delete dead code | C1 | 0.5 |
| 2 | Test harness (`WebApplicationFactory` + SQLite in-memory) and first regression tests | T1 | 1 |
| 3 | Fallback auth policy, per-action policies, full permission seed for admin and user, frontend interceptor | S1, D5, D6 | 1.5 |
| 4 | Rotate the JWT key, move it to secrets/env, `IOptions` + `ValidateOnStart` | S2 | 0.5 |
| 5 | Ownership from the token; remove `id` / `createdAt` / `employeeId` from inputs | S3, D9 | 1 |
| 6 | Availability: validation, bounds, correct overlap, capacity | S4, D2 | 1 |
| 7 | Overlap check + transaction on booking create/update; `start < end` | D1, D4 | 1 |
| 8 | Global exception handler + ProblemDetails; uniform login error; rate limiting on auth | A1, S5, S6 | 1 |
| 9 | Dev-only demo seed; Swagger dev-only; CORS from config | S7, S8 | 0.5 |

### Phase 2 — Platform (≈ 1 sprint, ~5–7 d)
| # | Item | Findings | Est. |
|---|---|---|---|
| 1 | Upgrade to .NET 10 / EF Core 10, Central Package Management, package cleanup | P1, P4 | 1.5 |
| 2 | EF migrations baseline; remove `schema.sql` bootstrapping; schema fixes migration | P2, P3, D11, D12 | 2 |
| 3 | Service-detail diff update; restrict + soft delete for catalog entities | D7, D8 | 1 |
| 4 | UTC / `DateTimeOffset` / `TimeProvider`; office time zone + working hours in data | D10, D4 | 1 |
| 5 | CI pipeline, analyzers, `TreatWarningsAsErrors` | T2 | 0.5 |

### Phase 3 — Domain and API maturity (~1.5 sprints)
| # | Item | Findings |
|---|---|---|
| 1 | `BookingPolicy` with duration, quota and capacity rules; `MinMax` split into ints | D3 |
| 2 | `Result<T>` across services; validators for all DTOs; `/api/v1` with cleaned-up routes, pagination and filtering | A2–A5 |
| 3 | Booking status / cancellation; audit log | D8, §4.7 |
| 4 | Health checks, structured logging, OpenTelemetry | O1 |
| 5 | Decide on PostgreSQL; if yes, Testcontainers + exclusion constraint | D1, §4.5 |

---

## 6. Decisions needed from product / lead

1. **Registration model:** is self-registration allowed at all? If yes, does a registered user automatically become an `Employee` who can book? Today they can't book (D5).
2. **Booking semantics:** what exactly do `Quantity`, `BookingPerTime` and `Capacity` mean? Units of a resource, concurrent bookings, or people per room? The rules in §4.3 depend on the answer.
3. **Deletion policy:** can an office or service with past or future bookings be deleted? (Proposal: no. Deactivate it instead.)
4. **Time zones:** one per office, or a single company-wide zone?
5. **Permission freshness:** is up to 2 hours of stale permissions after revocation acceptable? If not, use short-lived access tokens plus refresh tokens, or resolve permissions on the server per request with a cache.
6. **Target database / hosting:** stay on SQLite single-instance, or plan for PostgreSQL now?

---

## 7. What's already good (keep it)

- Clear project boundaries, with dependencies pointing in one direction.
- `CancellationToken` flows from the controller down to EF everywhere.
- `AsNoTracking` on read paths.
- Permission-claim based authorization design, centralized in `Permissions` constants. It only needs to be applied.
- Startup refuses a JWT key shorter than 256 bits.
- `PasswordHasher<T>` (PBKDF2) rather than hand-rolled hashing.
- Error codes (`Codes`) the frontend can switch on. Keep them inside ProblemDetails.
