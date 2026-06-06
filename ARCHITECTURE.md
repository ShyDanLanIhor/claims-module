# Architecture

## 1. Overview
This repository implements a vertical slice of a Claims Management module — First Notice of
Loss (FNOL) intake and Reserve Management — for an insurance Policy Administration System.
It is a greenfield, self-contained implementation built for a technical assessment.

In scope: FNOL claim creation, the claim lifecycle state machine, reserve management with
authority-based approval, Hangfire-driven GL posting simulation, document management
(Azure Blob), and an append-only audit log. Out of scope: coverage evaluation, payments,
subrogation, litigation, and fraud scoring.

## 2. Technology Stack
- Backend: .NET 9 / C# 13, ASP.NET Core Web API, MediatR 12 (CQRS), EF Core 9, FluentValidation 11, AutoMapper 13, Hangfire 1.8
- Orchestration & observability: .NET Aspire 9.5 (AppHost + ServiceDefaults — OpenTelemetry, health checks, resilience)
- Database: SQL Server 2022 / Azure SQL
- File storage: Azure Blob Storage, with a local filesystem fallback behind `IStorageService`
- Frontend: Angular 21 (standalone, **zoneless**, signals), Angular Material, Reactive Forms
- Cloud: Microsoft Azure (App Service or Container Apps), provisioned via azd / Bicep
- CI/CD: GitHub Actions

The .NET SDK is pinned in `global.json` to the 9.0.x line; the toolchain enforces this so the
backend always builds against .NET 9 even on machines where .NET 10 is also installed.

## 3. Solution Structure
Backend layers are grouped under `src/` as **core** (Domain, Application), **infra**
(Infrastructure, Persistence) and **api** (API); Aspire orchestration projects live under
`src/aspire`; the Angular client lives under `src/clients/web`. The .NET solution
(`ClaimsModule.sln`) contains seven C# projects (five Clean Architecture layers + AppHost +
ServiceDefaults) plus two test projects; the Angular app is a separate npm workspace.

```
src/core/ClaimsModule.Domain          Entities, value objects, enums, domain events, domain services
src/core/ClaimsModule.Application     CQRS commands/queries, validators, DTOs, interfaces, AutoMapper profiles
src/infra/ClaimsModule.Infrastructure Azure Blob + local storage, Hangfire jobs, clock
src/infra/ClaimsModule.Persistence    EF Core DbContext, configurations, migrations, repositories, UoW, audit, seed
src/api/ClaimsModule.API              Controllers, exception middleware, mock auth, composition root
tests/ClaimsModule.Application.Tests  Domain + validator unit tests (xUnit)
tests/ClaimsModule.Api.IntegrationTests  HTTP-level smoke test (WebApplicationFactory)
```

## 4. Clean Architecture & the Dependency Rule
Dependencies point inward, toward the domain:
- **Domain** — depends on nothing, *including no MediatR*. Entities, value objects, enumerations,
  domain events (a framework-agnostic `IDomainEvent`), and domain services (`ClaimLifecycle`,
  `ReserveAuthority`).
- **Application** — depends only on Domain. MediatR commands/queries + handlers, FluentValidation
  validators, DTOs, abstractions (repositories, `IUnitOfWork`, `IStorageService`, `IAuditLogService`,
  `IBackgroundJobScheduler`, `ICurrentUserService`, `IDateTime`), and AutoMapper profiles. It does
  **not** reference EF Core.
- **Infrastructure** — depends on Application; implements its interfaces (Azure Blob / local storage,
  the Hangfire scheduler and job definitions, the system clock).
- **Persistence** — depends on Application; EF Core `DbContext`, entity configurations, migrations,
  repositories, Unit of Work, claim-number generator and the audit-log writer.
- **API** — composition root; wires Application + Infrastructure + Persistence into DI, exposes
  controllers, the exception-handling middleware and the mock-auth `ICurrentUserService`.

The Aspire AppHost orchestrates the whole stack for local development — it provisions a SQL Server
container, runs the API (injecting the database connection string and waiting for the DB to be ready),
and runs the Angular dev server — with one command and a unified dashboard. ServiceDefaults centralises
telemetry, health checks and resilience. The API resolves its connection string from the Aspire-injected
`ConnectionStrings:claimsdb` when orchestrated, falling back to appsettings when run standalone; the
Angular dev-server proxy (`proxy.conf.js`) forwards `/api` to the Aspire-injected API address.

## 5. Data Model
Key entities (FRS §9), all carrying the standard conventions (sequential `UNIQUEIDENTIFIER` PKs,
`DECIMAL(19,4)` money, `DATETIMEOFFSET(7)` timestamps, soft-delete + audit columns, `OrganisationId`
tenant isolation; `RowVer` on aggregate roots).

- **Claim** — aggregate root. Owns its `LossEvent` (1:1), `ClaimParty` / `ClaimRiskObject` /
  `ClaimReserveComponent` / `ClaimDocument` collections. Holds status, denormalised policy fields,
  and the manager-override flag.
- **LossEvent** — the loss occurrence (date, description, location, cause code, estimate).
- **ClaimParty** — claimants/insured/third parties/witnesses/attorneys; `IsActive` models soft removal.
- **ClaimRiskObject** — affected assets (type + description).
- **ClaimReserveComponent** — aggregate root and second consistency boundary; one per component type
  per claim; holds the computed `CurrentAmount`.
- **ReserveHistory** — append-only reserve transaction ledger (event-sourced); carries approval and
  GL-posting state, the idempotency key and the per-component change sequence.
- **ClaimDocument** — document metadata (blob path, content type, size); bytes live in storage.
- **ClaimAuditLog** — immutable, append-only event log (no nav properties; indexed by claim + time).
- **CauseOfLossCode**, **Policy**, **ClaimStatusTransition** — seeded reference / simulated data.
- **ClaimNumberSequence** — per-org/year atomic counter backing claim-number generation.

Relationships and indexes are configured with `IEntityTypeConfiguration<T>` (no data annotations).
Global conventions are applied centrally in `ClaimsDbContext`: a soft-delete + tenant query filter for
every applicable entity, client-assigned sequential (COMB) GUID keys (`ValueGeneratedNever`, no store
default — see §10), `DECIMAL(19,4)` for all decimals, and `datetimeoffset(7)` for all timestamps. Enums are stored as strings. An EF `SaveChanges`
interceptor stamps audit columns and turns hard deletes into soft deletes.

## 6. CQRS Flow
Every state change is a MediatR **Command**; every read is a **Query**. A request flows:

```
Controller → ISender.Send(command) → ValidationBehaviour (FluentValidation) → Handler
   → domain method on the aggregate (raises domain events)
   → IUnitOfWork.SaveChangesAsync()
        → DbContext drains domain events → publishes DomainEventNotification<T>
        → audit handlers enlist ClaimAuditLog entries
        → single transactional SaveChanges commits aggregate + audit together
```

Validation is wired at the **pipeline** level (`ValidationBehaviour<TRequest,TResponse>`), not in
controllers, and failures surface as a structured 422. The write side uses repositories returning
tracked aggregates plus the Unit of Work; the read side uses separate `AsNoTracking` read
repositories and maps to DTOs with AutoMapper — a clean read/write split.

## 7. Domain Events
The Domain raises framework-agnostic events; the persistence layer wraps each in a
`DomainEventNotification<T>` and publishes it through MediatR **before** the transaction commits, so
audit entries are part of the same atomic save. Events: `ClaimCreated`, `ClaimStatusChanged`,
`ClaimClosed`, `ClaimReopened`, `ClaimPartyAdded/Removed`, `ClaimDocumentUploaded`,
`ReserveSubmitted` (+ auto-approval), `ReserveApproved/Rejected/Retracted`. Their handlers write the
corresponding audit-log entries through `IAuditLogService` — the single sanctioned writer for the
append-only log (FRS §14.2).

## 8. Background Jobs (Hangfire)
Jobs are defined in Infrastructure but reach data only through the `IBackgroundJobData` Application
abstraction, so the job code never depends on EF Core. The Application layer enqueues work through
`IBackgroundJobScheduler`, an abstraction over Hangfire.

- **PostGlReserveChangeJob** (`IGlPostingJob`, the FRS "PostGLReserveChangeJob") — enqueued *after the
  transaction commits* on auto-approval or manual approval. Idempotency key
  `Reserve:{ReserveComponentId}:Change:{ChangeSequence}`; re-entrant safe via a guard on
  `PostingStatus == Posted` and a unique index on the key. On success it writes one
  `GL_POSTING_SIMULATED` audit entry (the simulated DR/CR journal), sets `PostingStatus = Posted`, and
  commits both atomically. Transient failures are retried (`[AutomaticRetry(Attempts = 3)]`, a
  deliberate cap). **Terminal failure** is handled by `GlPostingFailureStateFilter`, an
  `IApplyStateFilter` that fires when Hangfire applies the `FailedState` (which only happens once
  retries are exhausted — it reschedules instead while attempts remain). It sets `PostingStatus = Failed`
  and writes one `GL_POSTING_FAILED` audit entry, satisfying FRS §12.1 without per-attempt noise.
- **SlaMonitoringJob** (`ISlaMonitoringJob`) — recurring every 15 minutes (`*/15 * * * *`). Flags
  Draft/Open claims not updated in 48 hours with an `SLA_BREACH_DETECTED` audit entry, de-duplicating
  so a claim is not re-flagged within 24 hours. Does not change claim status.

Hangfire's SQL schema is prepared **synchronously at startup** (before the app serves requests) so the
first reserve enqueue cannot race schema creation (`Invalid object name 'HangFire.Job'`).

## 9. Azure Architecture
Provisioned as Infrastructure-as-Code with **Bicep** under `iac/` (azd-compatible via `azure.yaml`).
`iac/main.bicep` is subscription-scoped (creates the resource group) and composes one module per resource:

| Resource | Module | Purpose |
|---|---|---|
| **App Service** (Linux, `DOTNETCORE\|9.0`, B1) | `modules/api.bicep` | hosts the API; system-assigned managed identity |
| **Static Web App** (Free) | `modules/web.bicep` | hosts the Angular SPA |
| **Azure SQL** (server + Basic DB) | `modules/sql.bicep` | the claims database |
| **Storage account + `claim-documents` container** | `modules/storage.bicep` | document blobs |
| **Key Vault** (RBAC) | `modules/keyvault.bicep` | holds the SQL + Storage connection strings |
| **Log Analytics + Application Insights** | `modules/monitoring.bicep` | telemetry sink for the OpenTelemetry/ServiceDefaults pipeline |

**Security posture:** no secrets live in app configuration. The SQL and Storage connection strings are
written to Key Vault by their modules; the API's app settings reference them as
`@Microsoft.KeyVault(SecretUri=…)`, and the App Service **managed identity** is granted *Key Vault
Secrets User* to resolve them at runtime. SQL uses "Allow Azure services" for the App Service; the
Storage connection string retains the account key because the app generates SAS URLs with a shared-key
credential. The storage provider is configuration-driven (`Storage:Provider` = `AzureBlob` |
`LocalFileSystem`), so the same code runs locally and in Azure. CORS on the API is set to the Static
Web App's origin; the frontend is built against the API's URL.

**Deploy:** one-shot with `azd up`, or via the CI/CD pipeline (§below / `.github/workflows/ci-cd.yml`).

## 10. Key Design Decisions & Tradeoffs
- **Five-layer Clean Architecture** per the specification; folders grouped `core/infra/api` for readability.
- **Domain kept free of MediatR.** Domain events implement a local `IDomainEvent`; the Application
  boundary wraps them in `DomainEventNotification<T>`. Costs a little ceremony, buys a Domain with
  zero framework dependencies.
- **Dispatch-before-save.** Domain events are published inside `SaveChangesAsync` before the base
  call, so audit entries commit in the same transaction as the change they describe — no second save,
  no risk of a change without its audit trail.
- **Reserves are event-sourced.** `ReserveHistory` is append-only; `CurrentAmount` is the sum of
  effective transactions. Adjustments and rejections are new rows, never mutations — matching the FRS
  and giving a complete, auditable financial history.
- **Authority is a domain concept.** `ReserveAuthority` encapsulates the three-tier thresholds, the
  aggregate $10M override and self-approval is enforced in the handler; tiering uses the transaction's
  magnitude so negative subrogation recoverables are handled correctly.
- **Atomic, gap-free claim numbers.** A pre-seeded per-org/year counter is incremented with a single
  `UPDATE … OUTPUT` inside the FNOL transaction; the row lock serialises concurrent submissions.
- **CQRS read/write split** with distinct repositories signals intent and keeps queries `AsNoTracking`.
- **Sequential (COMB) GUID identities.** Aggregates need their id before `SaveChanges` (child FKs and
  the reserve idempotency key reference it inside the transaction), so identity is assigned client-side.
  `BaseEntity` uses a SQL-Server-ordered COMB GUID (`SequentialGuid`) rather than random `Guid.NewGuid()`
  so the §15.1 "sequential GUIDs to avoid index fragmentation" intent holds. Every GUID `Id` is marked
  `ValueGeneratedNever()` and assigned client-side; there is **no** `NEWSEQUENTIALID()` store default
  (a store default combined with a client-assigned value made EF treat children added to a *loaded*
  aggregate as existing rows → spurious `409` concurrency errors, so it was deliberately removed).
- **CC-02 (no unresolved Critical issues at closure) is enforced preventively.** The FRS entity model
  defines no validation-issue table, so rather than invent a store, Critical issues are blocked at
  creation (FluentValidation) and at the Draft→Open gate (BR-ST-02); a claim therefore cannot reach a
  closable state carrying one. CC-01/CC-03/CC-04 are checked explicitly at closure.
- **Reserve transactions use one POST with a `TransactionType` (Add/Adjust/Recover), not a separate
  `PUT /reserves/{id}` for adjustments.** Every reserve change is an append to the event-sourced
  `ReserveHistory`, so an "adjust" is semantically another transaction, not an in-place edit of an
  existing row. A single `POST /api/claims/{id}/reserves` carrying the type keeps the API aligned with
  the event-sourced model and FRS §10.2; the Assessment's example verb table (which lists a PUT for
  adjust) is satisfied behaviourally by the typed POST.
- **FNOL validation issues are tiered: blocking (Critical) vs non-blocking (Warning).** Critical issues
  (e.g. unknown/inactive cause code, description too short) are FluentValidation failures and return 422.
  Warnings (e.g. loss date outside the policy effective period — BR-C-02, or no risk objects) are written
  to the audit log and returned in `CreateClaimResult.Warnings`, but do **not** block creation — letting a
  handler record a claim with incomplete information (FRS §5.4). The Draft→Open transition requires an active
  Claimant party (BR-ST-02) and, per **BR-C-02**, that the policy-period warning be acknowledged: the gate
  **re-derives** the warning (loss date vs the linked policy's effective period) at transition time and
  blocks Open with a 422 unless the caller passes `acknowledgeWarnings` — so the rule is enforced without a
  persisted validation-issue store (the FE surfaces an acknowledgement dialog on that 422 and retries).
- **Global reference tables are intentionally not tenant-scoped.** `CauseOfLossCodes` and
  `ClaimStatusTransitions` are system-wide lookups, so they omit `OrganisationId`; every *tenant*
  business table carries it. (A deliberate exception to the §15.1 "every business table" wording.)
- **`ReserveHistory` and `ClaimAuditLog` are append-only event logs** and intentionally omit the
  `UpdatedAt`/soft-delete audit columns (meaningless on immutable rows); the creator is captured via
  `SubmittedByUserId`/`CreatedByUserId` per FRS §9.6/§9.8. The seeded global lookups
  (`CauseOfLossCodes`, `Policies`, `ClaimStatusTransitions`, `ClaimNumberSequences`) likewise omit the
  full audit-column quartet: they are immutable reference data applied via `HasData`, not per-tenant
  mutable business rows — a deliberate, scoped exception to the §15.1 "all tables" wording.
- **`ReserveHistory.PostingJobId`** holds the real Hangfire job id, captured from the value returned by
  `IBackgroundJobScheduler.Enqueue(...)` at enqueue time (no `PerformContext` leak into the Application
  layer) and persisted on the transaction with a second `SaveChanges`. This gives operators a direct
  Hangfire-dashboard lookup per GL posting (FRS §12.1). `MarkPosted` only flips `PostingStatus`.
- **GL posting uses an explicit `[AutomaticRetry(Attempts = 3)]`** rather than Hangfire's default (10):
  three attempts reach the terminal `GL_POSTING_FAILED` state (and surface the **Retry GL** action)
  promptly without excessive churn — a deliberate, modest deviation from the FRS "default retry policy".
- **HTTP `Idempotency-Key` (FRS §10).** Write requests (POST/PUT/PATCH/DELETE) carrying an
  `Idempotency-Key` header are de-duplicated by `IdempotencyFilter`: the first 2xx response is recorded
  per-tenant in `IdempotencyRecords` and replayed verbatim on a retry, so the operation executes once.
  This complements the domain-level idempotency (atomic claim-number counter; GL key
  `Reserve:{id}:Change:{seq}` — the posting job no-ops on replay).
- **`ClaimAuditLog` immutability is enforced in the application** (all writes go through the Add-only
  `IAuditLogService`; no update/delete path exists). A database-level `REVOKE UPDATE/DELETE` / dedicated
  restricted role (FRS §14.2 wording) was not added for this single-credential slice.
- **BR-R-05 ($10M aggregate) is enforced as a hard `422` unless a Manager applies the override**, rather
  than a soft "warning". The Manager override is required to proceed either way (matching the assessment's
  BR-R-07), so the block is the safer reading.
- **Minor convention notes:** `BIT` flag columns are `NOT NULL DEFAULT 0` (false-default flags — `IsDeleted`,
  `IsPrimary`, `ManagerOverrideApplied` — carry a DB-level default; EF still supplies the value on insert);
  `POST .../parties` and `.../documents` return `201 Created`; there is no standalone FNOL `validate`
  endpoint — validation runs inside `POST /api/claims` and returns the same structured 422.
- **.NET Aspire** for one-command local orchestration and built-in observability; minor overhead for a
  single-service slice, accepted for the telemetry and dev ergonomics.
- **Known dependency advisories (assessed & accepted):**
  - `AutoMapper 13.0.1` carries advisory **GHSA-rvv3-g6hj-g44x** (stack-overflow DoS via ~25k-deep
    self-referential graphs). Not reachable here — DTOs are not self-referential and mapping inputs are
    trusted internal entities, never attacker-supplied graphs. 13.0.1 is the last MIT-licensed release;
    the fix lands in 15.1.1+, which is AutoMapper's commercial-licensing line. Decision: stay on 13.0.1
    for this assessment; upgrade path is documented.
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.12.0` (NU1902, moderate) arrives transitively via
    the Aspire 9.5 ServiceDefaults template; left aligned with the template to avoid destabilising the
    OTel package set.

## 11. Error Handling
`ExceptionHandlingMiddleware` maps exceptions to the consistent error body from FRS §10.4:
`ValidationException` / `BusinessRuleException` / `DomainException` → 422 (with field-keyed `errors`);
`NotFoundException` → 404; `ForbiddenAccessException` → 403; `DbUpdateConcurrencyException` → 409;
anything else → 500 (logged, with a `traceId`).

## 12. Security / Multi-tenancy
A mock auth scheme (FRS §3) reads the caller's identity and role from request headers
(`X-User-Id` / `X-User-Role` / `X-User-Name`) via `ICurrentUserService`; the Angular interceptor will
supply these along with the Bearer token. Reserve approval endpoints enforce role + authority
server-side. All data is scoped to a single seeded `OrganisationId`; a correlation id is propagated to
every audit entry within a request.

## 13. Frontend (Angular)
A standalone, **zoneless** Angular 21 app (`src/clients/web`) using signals and Reactive Forms,
talking to the API exclusively through a typed service layer (`core/services/*-api.service.ts`) — no
direct HTTP in components. Three lazy-loaded feature routes:
- **Claims list** — paginated, filterable table (status / cause / loss-date range / search), colour-coded
  status badges, row navigation, "Log New Claim".
- **FNOL intake** — a 3-step `MatStepper` reactive form (policy & loss → parties & risk objects →
  initial reserve & review) with policy typeahead, an in-force badge, a live authority-threshold
  indicator, per-step validation and a review summary.
- **Claim detail** — header with status chip + contextual status-transition menu (confirmation dialog),
  and Overview / Parties / Reserves / Documents / Audit Log tabs. The Reserves tab adds/approves/
  rejects/retracts transactions (approve/reject gated to Supervisor+), shows GL posting status, and
  summary cards.

Cross-cutting: functional HTTP interceptors for the mock-auth Bearer + identity headers, a global
error→snackbar handler (parses the structured 422 body), and a request-counting loading bar. A mock
`AuthService` provides Handler/Supervisor/Manager users with a header role-switcher; role gating hides
approval actions. Material M3 theming (azure palette).

## 14. Testing
- **Unit tests** (xUnit) cover the domain rules that carry the most risk: reserve authority tiers,
  the lifecycle state machine, the reserve-ledger aggregate behaviour, and the FNOL validator.
- **Integration test** boots the real HTTP pipeline with `WebApplicationFactory` (EF InMemory,
  Hangfire server disabled) and exercises a controller → MediatR → handler round-trip.

## 15. CI/CD
`.github/workflows/ci-cd.yml` (GitHub Actions, `workflow_dispatch` + push to `main`) runs the four
required stages and authenticates to Azure with **OIDC federated credentials** (no stored cloud secret):
1. **Build & test** the .NET solution (`dotnet build` + `dotnet test`) and **build the frontend** (`ng build`).
2. **Provision** infrastructure (`az deployment sub create` against `iac/main.bicep`), capturing resource
   names/URLs from the deployment outputs.
3. **Run EF Core migrations** — opens a transient SQL firewall rule for the runner IP, `dotnet ef database
   update`, then removes the rule.
4. **Deploy** the API (`azure/webapps-deploy`) and the Angular app (`Azure/static-web-apps-deploy`), rebuilding
   the frontend against the deployed API URL first.

Configuration: variables `AZURE_ENV_NAME` / `AZURE_LOCATION`; secrets `AZURE_CLIENT_ID` / `AZURE_TENANT_ID`
/ `AZURE_SUBSCRIPTION_ID` (OIDC) and `AZURE_SQL_ADMIN_PASSWORD`.
