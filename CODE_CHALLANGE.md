# Code Challenge Analysis

What an interviewer is likely to ask you to build, fix or explain in this repository, based on the
current state of the code. Each item says **where** the problem is, **what a good answer looks
like**, and the **follow-up questions** to expect.

The repo is a good interview vehicle because it is small, layered (Controller → Service → EF Core on
the backend; page → store → use case → repository on the frontend) and has many *deliberate-looking*
gaps. The skipped tests in `backend/test/**` (`KnownIssue.Prefix`) are effectively a list of
challenges already written for you; un-skipping one and making it pass is the most likely task.

---

## How interviewers usually frame it

| Format | Typical time | What they watch |
|--------|--------------|-----------------|
| Live bug fix ("this test is skipped, make it pass") | 20–40 min | Reading code, finding root cause, not breaking other tests |
| Small feature ("add cancel booking") | 45–90 min | End-to-end slice: API, service, UI, test |
| Code review ("what would you change here?") | 15–30 min | Seeing security / correctness issues, prioritising |
| Design discussion ("how would this scale?") | 15–30 min | Trade-offs: SQLite, replicas, concurrency, auth |

Rule of thumb: **write the failing test first, fix the smallest thing, run the whole suite.**
Backend: `dotnet test` (102 pass / 19 skipped today). Frontend: `npx ng test --watch=false` (39 pass).

---

## Tier 1 — Most likely (core business rules)

### 1. Prevent double booking (overlap check) — `D1` ✅ implemented

> Done in feature `specs/003-booking-overlap-check`: `CreateAsync` returns `Codes.BookingConflict`, the API
> answers **409** `ERR_BOOKING_CONFLICT`, and the booking dialog shows the message. Still worth
> discussing: the same check in `UpdateAsync`, race conditions, and a shared database across pods.

- **Where:** `backend/src/booking.infrastructure/Services/BookingService.cs` → `CreateAsync`, `UpdateAsync`.
  Test: `BookingEndpointsTests` / infrastructure `BookingService` tests, skipped `D1`.
- **Problem:** two bookings for the same `ServiceDetailId` and overlapping time are both accepted.
- **Good answer:**
  - Overlap rule: `existing.Start < new.End && new.Start < existing.End` (half-open intervals, so
    10:00–10:30 and 10:30–11:00 do **not** collide).
  - Query with `AnyAsync` on the same `ServiceDetailId`, excluding the booking's own id on update.
  - Return a domain error code (e.g. `Codes.Conflict`) and map it to **409 Conflict** in the controller.
- **Follow-ups:** *"Two requests arrive at the same millisecond — what happens?"* (race condition:
  check-then-insert isn't atomic; use a transaction with `Serializable` isolation, a unique
  constraint on a slot table, or optimistic concurrency). *"What if there are 3 replicas with
  per-pod SQLite?"* (each pod has its own DB, so the check is meaningless across pods — needs a shared DB).

### 2. Validate booking input — `D4`, `A1/D9`
- **Where:** `BookingService.CreateAsync`, `booking.core/DTOs/Booking.cs`, `booking.api/Utils/FluentValidation.cs`.
- **Problem:** no check that `start < end`; an unknown `employeeId` / `serviceDetailId` throws a
  `DbUpdateException` → raw **500**.
- **Good answer:** a `BookingUpsertDtoValidator` with FluentValidation (already referenced in
  the project, no new package), plus `AnyAsync` existence checks in the service that return
  `NotFound`/`BadRequest` codes. Bonus: also enforce the same rules as the frontend modal
  (business hours, weekdays, max 7 weeks ahead, not in the past).
- **Follow-up:** *"Should validation live in the frontend, the backend, or both?"* — both; the
  backend is the source of truth, the frontend is UX.

### 3. Fix the availability slots — `D2`, `S4`
- **Where:** `BookingService.GetAvailability`.
- **Problems:**
  - Only the slot **start** is checked, so a booking 10:15–10:45 leaves the 10:00 slot "available" (`D2`).
  - `frequency <= 0` → **infinite loop** (`S4`) — a DoS bug.
  - `DateTime.Parse` / `int.Parse` on bad input → 500.
  - Hours are hard-coded `08:00–15:00`, but the frontend modal enforces `09:00–17:00`.
- **Good answer:** treat each slot as `[time, time + frequency)` and use the overlap rule from #1;
  validate `frequency` (e.g. 5–240) and use `TryParse` → 400; move opening hours to configuration
  (`IOptions<BookingHoursOptions>` in `appsettings.json`) shared by both rules.
- **Follow-up:** *"What's the complexity?"* O(slots × bookings) — fine for a day; mention sorting
  bookings and sweeping if asked to optimise.

### 4. Enforce service rules that exist in the schema but are never used
- **Where:** `Service` table has `quantity`, `max_bookings_week`, `booking_per_time`, `min_max`
  (`schema.sql`, `AppDbContext.cs`); `BookingService` ignores them.
- **Likely ask:** *"An employee may book at most N times per week for a service"* or *"bookings must
  be between min and max minutes"*.
- **Good answer:** count bookings for `(EmployeeId, ServiceId)` in the ISO week of `start`
  (Monday–Sunday) and reject with 409/422 when `>= MaxBookingsPerWeek`. Parse `min_max` once and
  explain the format you assumed.

---

## Tier 2 — Security (very common in code-review rounds)

### 5. Protect the endpoints — `S1`
- **Where:** every controller in `backend/src/booking.api/Controllers/`. `Program.cs` defines
  12 permission policies (`Booking.Read`, `Service.Create`, …) but **no controller uses them**;
  every endpoint is anonymous.
- **Good answer:** `[Authorize]` at controller level + `[Authorize(Policy = Permissions.Bookings.Create)]`
  per action, `[AllowAnonymous]` on login/register and `/healthz`; or a global
  `FallbackPolicy = RequireAuthenticatedUser()`. Then fix the seeding gaps it exposes:
  admin has only 2 of 12 permissions (`D6`) and role `r_user` has none, so registered users can't book (`D5`).
- **Follow-up:** *"Roles vs. permission claims — why both?"*

### 6. Employees can act on other people's bookings
- **Where:** `BookingUpsertDto.employeeId` is sent by the client (`booking-modal.component.ts` →
  `authStore.getEmployeeId()`).
- **Good answer:** read `employeeId` from the JWT claim on the server and ignore/forbid the body
  value; on update/delete check ownership (owner or admin) → 403. This is a classic **IDOR** question.

### 7. User enumeration on login — `S5`
- **Where:** `AuthService.LoginAsync` returns `UserNotFound` vs `InvalidCredentials`.
- **Good answer:** return one generic `InvalidCredentials` / 401 for both, ideally with similar
  timing (hash a dummy password when the user doesn't exist).

### 8. Other review points to mention
- JWT key lives in config; in Kubernetes it must come from the `Secret` (`backend/deploy/kubernetes/secret.example.yaml`).
- Token stored in `localStorage` on the frontend → XSS exposure; discuss httpOnly cookies as the alternative.
- No rate limiting on `/api/Auth/login` (.NET 8 has built-in `AddRateLimiter`, no new package).

---

## Tier 3 — API correctness and data integrity

| ID | Where | Problem | Expected fix |
|----|-------|---------|--------------|
| A3 | `BookingController.GetByServiceId` | Service with no bookings → **404** | Return **200** with `[]`; 404 only if the *service* doesn't exist. (Frontend currently maps 404 → `[]` to work around it — remove that once fixed.) |
| — | `BookingService.CreateAsync` | `CreatedAt = dto.createdAt` → client controls it; new bookings get `0001-01-01` | Set `CreatedAt = DateTime.UtcNow` on the server; drop it from the DTO. Also: `Id = dto.id` lets the client pick the PK. |
| D7 | `ServiceService.UpdateAsync` | Deletes and recreates all details → ids change, FK error if they have bookings | Diff the collections: update matching ids, add new, remove only unreferenced ones. |
| D8 | Office/Service delete | FK `DbUpdateException` → 500 | Check dependants first and return 409, or define cascade/soft delete deliberately. |
| D11 | `Office.image` | `NOT NULL` in SQL, optional in DTO → 500 | Make DTO required (validator) or column nullable — pick one and explain. |
| D13 | `DbInitializer.SeedAsync` | Demo booking not seeded on a fresh DB (lookups before `SaveChangesAsync`) | Save parents first, or build the graph with navigation properties. |
| — | `ServicesController.GetAll` | Ignores `officeId`; frontend filters client-side | Add `GET /api/Services?officeId=` filtered in SQL. |
| — | Times | `DateTime` without kind; browser sends local time | Store UTC, convert at the edges; be ready to discuss time zones. |

Also likely: **"Add a global exception handler"** — `app.UseExceptionHandler` + `ProblemDetails`
(`AddProblemDetails()` is built in) so no endpoint ever returns a raw stack trace.

---

## Tier 4 — Feature requests (full-stack slice)

These test whether you can follow the existing architecture end to end.

1. **My bookings page** — `GET /api/Booking/me` (employee from JWT) → new use case
   `GetMyBookingsUseCase` → repository method → page at `booking/my-bookings` with a `mat-table`.
2. **Cancel a booking** — `DELETE /api/Booking/{id}` exists; add ownership check (#6), a
   `CancelBookingUseCase`, a confirm dialog, refresh the table. Only future bookings cancellable.
3. **Show free slots in the modal** — the backend `POST /api/Booking/availability` is **never
   called** by the frontend. Replace the free-text time input with a `mat-select` of available slots
   for the chosen date, disabling taken ones.
4. **Admin CRUD for offices/services** — `RoleGuard` exists but routes to a non-existent
   `/not-authorized`; no admin pages exist. Add a guarded `admin` route, a `not-authorized` page and
   a form using the existing `Create/Update` endpoints.
5. **Pagination / filtering** on `GET /api/Booking` (`?page=&pageSize=&from=&to=`) with
   `Skip/Take` and a total count; `mat-paginator` on the frontend.
6. **Logout + guarded routes** — `AuthGuard` exists but is **commented out** in `app.routes.ts`;
   `booking/**` is reachable without logging in. Wire the guard (functional `CanActivateFn`
   is the modern form), add a logout button in the toolbar, handle 401 in the interceptor
   (clear session → redirect to login).

---

## Tier 5 — Frontend code-quality questions

Expect *"walk me through this component, what would you change?"* on these:

- **`booking.store.ts`**
  - `checkRouteState()` creates **new `combineLatest` subscriptions on every `NavigationEnd`** and
    never unsubscribes → memory leak and duplicate work. Fix: subscribe once, or derive state with
    `toSignal`/`computed` from route params.
  - Mix of `BehaviorSubject` and signals for the same kind of state — pick one (signals).
  - `setSelectedOfficeId` guard `!this.selectedOfficeId() && this.selectedOfficeId()?.id === id`
    can never be true (logic bug); intended `this.selectedOfficeId()?.id === id`.
  - Leftover `console.log` and commented "TESTING" code.
- **`booking-modal.component.ts`**
  - `form.valueChanges.subscribe` without teardown; `data: any`, `Subject<any>`.
  - Business rules (9–17h, weekdays, 7 weeks) duplicated client-side and inconsistent with the
    backend (8–15h) → single source of truth.
  - Uses `new Date()` directly → hard to unit test; inject a clock or use `jasmine.clock()`.
  - Error shown via snackbar *and* `errorMsg` — two error channels.
- **`service-detail.component.ts`**
  - Manual `Subscription[]` array → use `takeUntilDestroyed()`.
  - `var id`, `any` types, the `openModal` subscriptions are never cleaned up.
- **Architecture talking points:** why a `domain` / `data` / `presentation` split with use cases
  that only forward to a repository (is it over-engineering for this size?); `OnPush` + signals;
  environment-based `apiUrl`; functional interceptors.

---

## Tier 6 — DevOps / design discussion

Because the repo ships `deploy/` folders, expect questions such as:

- **"The HPA scales the API to 5 pods. What breaks?"** Each pod has its own SQLite in an
  `emptyDir` → users see different data per pod, bookings vanish on restart, and the overlap check
  (#1) can't work. Answer: one shared database (PostgreSQL/SQL Server) or a single replica with a
  `PersistentVolumeClaim`; cookie affinity is only a demo workaround.
- **"How do you run migrations in Kubernetes?"** `schema.sql` is applied on startup by every pod —
  discuss a Job / init container, and EF migrations.
- **"How do you roll out the JWT secret?"** `Secret` + `envFrom`, never in the ConfigMap or image.
- **Readiness vs. liveness probes**, resource requests vs. limits, why the containers run non-root.
- **Observability:** structured logs, `/healthz` checking the DB (`AddDbContextCheck` needs a package
  — mention the constraint in `.specify/memory/constitution.md`: no new libraries).

---

## Suggested preparation order

1. Un-skip and fix **D1 (overlap)**, **D4 (start < end)** and **A3 (empty list → 200)** — they are
   short and the most probable live tasks.
2. Fix **S4 (availability infinite loop / parsing)** and **D2 (slot overlap)**.
3. Add **`[Authorize]` + permissions (S1, D5, D6)** and **employee-from-JWT (IDOR)**.
4. Build **My bookings + Cancel** end to end (backend endpoint, use case, page, tests).
5. Wire **AuthGuard + logout + 401 handling** on the frontend.
6. Refactor **`BookingStore`** subscriptions; be ready to explain signals vs. RxJS.
7. Rehearse the **scaling/SQLite** answer for the deployment discussion.

For each one, be ready to say in one sentence: *what was wrong, why it matters to a user, how you
proved the fix (which test).*
