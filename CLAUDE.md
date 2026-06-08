# CLAUDE.md — Claims Module (DICEUS Fullstack assessment)

Operational guide for working in this repo. Read this first.

## What this is
A greenfield vertical slice of an insurance **Claims Management** module — **FNOL intake + Reserve
Management** — built for the DICEUS Fullstack (.NET 9 + Angular) technical assessment. The two
confidential spec docs are in `docs/*.docx` (gitignored; do not commit). Scope, entities, business
rules and conventions all come from those.

## Current status (2026-06)
### 🚀 DEPLOYED & LIVE ON AZURE (2026-06-09) — the assessment is fully delivered
- **Live URLs:** Frontend (SWA) https://jolly-ocean-0db0ca80f.7.azurestaticapps.net · API (App Service)
  https://app-api-we6lhaipoe6g4.azurewebsites.net (`/swagger`, `/health`). **Region: Poland Central**
  (the ONLY region that accepted trial SQL — West/North Europe, eastus2, westus2 all gated for free trial).
  RG `rg-claims-prod`: app-api-we6lhaipoe6g4, sql-we6lhaipoe6g4.database.windows.net/`claimsdb`,
  `stwe6lhaipoe6g4` (Blob), `kv-we6lhaipoe6g4`, `swa-we6lhaipoe6g4`, Log Analytics/App Insights.
- **Repo: github.com/ShyDanLanIhor/claims-module (PUBLIC** — made public so branch protection works on the
  free plan; security-swept, no secrets). **`main` is PROTECTED**: PR-only (no direct push, enforce_admins),
  required check "Build & test", linear history, no force-push/deletion. **Future changes MUST go via PR**:
  branch → PR → green "Build & test" → squash-merge → that merge's push deploys. `gh` authed as ShyDanLanIhor.
- **Azure:** sub "Azure subscription 1" `52c92884-8b2c-4274-84ca-61c12c47aa6c`, tenant
  `90bbbe61-b821-4552-9096-5c24750647a2`, **FREE TRIAL** (spending limit ON → no real charges; **30-day window**).
  OIDC app reg clientId `7fe0d9b6-96c1-4a49-8f57-9602eda0f720` (Owner on sub; federated subject
  `repo:ShyDanLanIhor/claims-module:environment:production`). GitHub vars AZURE_ENV_NAME=`claims-prod`,
  AZURE_LOCATION=`polandcentral`; secrets AZURE_CLIENT/TENANT/SUBSCRIPTION_ID + AZURE_SQL_ADMIN_PASSWORD
  (SQL pw saved at `%TEMP%\deploy-sql-password.txt`). **Tear down after demo:** `az group delete -n rg-claims-prod --yes`.
- **⚠️ TOOLING (post-compact gotcha):** `az` + `gh` are installed (winget) but **NOT on the tool-shell PATH**
  (installed after a prior session started). Prepend each call:
  `$env:PATH="C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin;C:\Program Files\GitHub CLI;$env:PATH"`. Both authed.
  Trigger a deploy: `gh workflow run ci-cd --ref main` (a push/merge to main also triggers; after a force-push
  the push itself triggers the run — don't also dispatch or you get a duplicate).
- **Deploy/CI bugs found+fixed (all in `.github/workflows/ci-cd.yml` + `Program.cs` + SWA config):** (1) Hangfire
  dashboard was mapped unconditionally → SQL init crash on the infra-free test host/CI runner → gated behind
  `Hangfire:EnableServer` (Program.cs). (2) Trial-SQL region gating → switched to Poland Central. (3) ARM mangles
  deployment-output casing (`AZURE_RESOURCE_GROUP`→`azurE_RESOURCE_GROUP`) → jq lowercases keys + rerun-safe
  deploy name (run_id+attempt). (4) **SWA SPA deep-link/refresh 404** → added `src/clients/web/public/staticwebapp.config.json`
  (`navigationFallback`→`/index.html`); first version used `**` (SWA allows ≤1 `*`) → corrected to single-`*`.
- **Live UI verified comprehensively in a real browser (Claude in Chrome, deviceId `573e9d11-…`):** ALL §11 on
  prod — dashboard (every filter, pagination, 7 badge colours, empty-state), FNOL create (policy typeahead +
  in-force badge), 5 tabs, **Azure Blob document upload + SAS download (200)**, reserves (auto+manual approve→GL
  Posted via cloud Hangfire, reject/retract, role-switch+gating, self-approval-422), notes save, add/remove party,
  audit + related-link tab-jump, transitions (Draft→Open, Open→Closed-w/-justification, Closed→Reopened→Open),
  422 amber-warn snackbar, CORS GET/POST/PUT/DELETE, responsive. Chrome-automation notes: mat-flat-button clicks
  via injected JS DON'T fire (use find-ref or coordinate clicks); reactive-form text inputs need REAL keystrokes
  (synthetic setVal is unreliable on the deployed bundle); renderer froze intermittently on long JS/some screenshots.
- **OPEN minor cosmetic bug (not fixed):** party name renders "Wendy null" when lastName is null — template
  `firstName + ' ' + lastName`. One-line fix in `claim-detail.html` → via PR. Low severity.

- **Backend**: complete & green (Domain/Application/Persistence/Infrastructure/API). All FRS endpoints,
  business rules, GL/SLA Hangfire jobs, audit, conventions. `dotnet build` 0 errors; **67 unit + 9 integration + 12 FE tests pass**
  (unit: domain + ALL handler business rules — ApproveReserve self-approval/authority/BR-R-05, SubmitReserve BR-C-06/auto-GL,
  ChangeClaimStatus BR-ST/BR-C-02/closure CC-01/CC-04, RetractReserve, ManageParties last-claimant, validators — plus
  PostGlReserveChangeJob re-entrancy & SlaMonitoringJob 24h-dedup via fakes (Application.Tests now references Infrastructure);
  integration: tenant filter, idempotency store, reference endpoints, + `ErrorContractTests` (bad-enum→422 §10.4/F2, invalid-create→422, unknown→404)).
  Test-only fix: `CustomWebApplicationFactory` gave the InMemory provider its own internal service-provider — it previously
  left SqlServer registered too, so any DB-touching HTTP test 500'd with "single database provider" (latent; my new tests caught it).
  10 verified requirement-reconciliation passes (only the live Azure deploy remains). Pass #10 (8-agent full
  sweep) — all dimensions + error-handling verifier + critic converged; 2 minor hardening items FIXED:
  `JsonStringEnumConverter(allowIntegerValues: false)` so crafted integer enum payloads (e.g. {"component":99})
  are rejected 422 not silently coerced; and `CreateClaim` InitialReserve validator gains `NotEqual(0m)`
  (matches SubmitReserve, blocks a zero subrogation initial reserve). Locked with an integer-enum integration
  test + a zero-initial-reserve validator test.
  **Architecture review (separate from spec-recon):** 8-agent adversarial sweep (6 dimensions → skeptic per
  finding) raised 24, confirmed 19 real+spec-safe, rejected 5 false-positives. ALL 19 FIXED in 3 phases
  (build 0 errors; 67 unit/WSL + 9 integration/WSL + 12 FE; `ef` migration consistent): LAYER-01 identity consts→
  `Domain/SeedIdentifiers`; CQRS-01 GL triplet→`ReserveGlPosting.ScheduleAsync`; DM-01/CQRS-02 BR-R-05→
  `Claim.WouldExceedAggregateLimit`+`ReserveOverride.Ensure`; CQRS-03 AutoMapper `AssertConfigurationIsValid` test;
  CQRS-04 deleted orphaned map; CQRS-05 `UploadClaimDocumentCommandValidator`; CQRS-06 FNOL party-email;
  DM-03 `internal/private set` entity encapsulation; DM-04 membership guards; AIX-01 `Response.HasStarted` guard;
  AIX-02 background audit=system actor; PERSIST-01 filtered unique index (GL idempotency at DB);
  PERSIST-02 `IStorageService.DeleteAsync` orphan-blob compensation; PERSIST-03 `UseQuerySplittingBehavior`;
  FE-02 (LIVE BUG) audit-pagination `effect`→`toObservable(id).switchMap`; FE-03 `takeUntilDestroyed`;
  FE-04 shared `reserve-authority.ts`; FE-05 de-duped claim GET. Pass #9 (8-agent full
  sweep + test-quality verifier) found 3 minor genuine items, all FIXED: DocumentsController empty-file guard
  now throws `BusinessRuleException`→§10.4 422 (was bare `BadRequest` string/400); `ClaimNumberGenerator`
  out-of-range-year fallback INSERT now supplies the client COMB `Id` (was omitted → would fail for years
  outside seeded 2024–2035, latent until 2036); FNOL adds a `required` mat-error on loss description. The
  test-quality verifier found NO false-confidence tests. Pass #8 (8-agent full
  adversarial sweep of FRS+assessment vs current code + this-cycle FE-change verifier + completeness critic)
  CONVERGED: all 6 dimensions + FE-change verifier clean, zero regressions; the only finding
  (`CauseOfLossCode.IsActive` lacked a `BIT DEFAULT`) was FIXED — `HasDefaultValue(true)` added to the config,
  the single `InitialCreate` migration and the snapshot (nothing deployed yet), so every BIT NOT NULL now
  carries a default per §15.1. Extras added earlier this
  cycle: HTTP `Idempotency-Key` (global `IdempotencyFilter` + `IdempotencyRecords`), App Insights wiring,
  Swagger+/health served in Production, real Hangfire `PostingJobId`, BIT `DEFAULT 0`, BR-C-02 acknowledge-before-Open,
  editable notes / add-remove party / audit pagination, single squashed `InitialCreate` migration.
  **Pass #7** (fresh adversarial review w/ re-extracted specs) found+fixed 8 items the earlier 6 passes missed:
  `PostingJobId` now recorded on the FNOL initial-auto-reserve path too (was discarded); model-binding/shape errors
  now return the §10.4 422 envelope (was framework 400) via `InvalidModelStateResponseFactory`; summary JSON key
  `totalReserve`→`totalReserves` (§10.1); FNOL Create gated on step2 too; FNOL Step 1 now shows policy coverage
  types (§5.2); mock-auth default GUID moved to `SeedConstants.HandlerUserId` (no hard-coded GUID, §15.4);
  `SequentialGuid` doc-comment corrected; ARCHITECTURE deviation list extended. Rejected 2 false agent claims
  (IdempotencyKey unique index DOES exist; BR-R-05 hard-422 is the correct realization, not a divergence).
  Then closed all 4 optional §11 UI-fidelity items: searchable cause-of-loss (`mat-select` + sticky filter
  panel, `panelClass=searchable-panel`); audit "related entity" is now a tab-jump link (ClaimParty→Parties /
  ReserveHistory→Reserves / ClaimDocument→Documents via a `selectedTab` signal); custom M3 palette generated
  from brand colours via `ng g @angular/material:m3-theme` → `src/clients/web/src/_theme-colors.scss`
  (**NEW untracked file — `git add` on next amend**), wired into `mat.theme`; assigned-handler dashboard filter
  is now a `mat-autocomplete` text search over the mock users (type→filter→select sets `assignedHandlerId`;
  clear removes filter). `ng build` clean; backend untouched.
- **Frontend**: complete (Angular 21, standalone, **zoneless**, signals + Material). Dashboard, multi-step
  FNOL form, claim detail (5 tabs), typed API service layer, interceptors, mock auth + role switch. `ng build` clean.
  **`ng test` (Vitest via `@angular/build:unit-test`) — 12 tests pass**: `auth.service` (role order/switch/token),
  `status-badge` (label + data-status), `claims-api` (HttpTestingController: list query/defaults, changeStatus PUT,
  removeParty DELETE), + scaffold `app.spec`. FE tests are optional per §8.3 — added as a showcase, not required.
  **Live UI review (2026-06, three passes via Claude Preview MCP driving the running client + an 8-agent
  adversarial §11 code review):** found+fixed **3 runtime bugs** + 7 low-severity §11-fidelity gaps (all 3 bugs
  caught only by actually clicking through — a static read can't see them). BUG 1: the Audit Log tab went
  stale after in-session reserve/upload actions — `reloadReserves()` and the upload path refreshed
  reserves/detail but not the audit list; both now reload audit (re-verified live: 3→8 entries, no reload).
  BUG 2: the FNOL Step-1 in-force badge never appeared when the loss date changed — its `computed()` read the
  loss date off the form control (a non-signal read) so it only re-evaluated on policy change, not date
  change; fixed by mirroring the date into a `signal` (verified live: badge turns green on date entry).
  BUG 3: a claim with open reserves could **never be closed via the UI** — CC-04 needs a justification but the
  Closed confirm dialog showed its reason field only `@if (requireReason)` and Closed set it false → no field,
  always 422; fixed by requiring/showing the justification when `hasOpenReserves` (verified live: $53k-open
  claim now closes). Fidelity fixes: FNOL future-date `mat-error`, FNOL review-summary now lists
  est-loss/police-report + per-item parties/risk-objects, FNOL server errors now bind inline to the relevant
  control (not just the top banner), party-role rendered as a badge, Add-Reserve is a toggle-opened slide-in
  panel, and the error interceptor maps severity (4xx→amber warn, 5xx/0→red error + new `snack-warning`).
  Live walkthrough (exhaustive — incl. data seeded for pagination + a forced Failed GL posting) CONFIRMED
  working per spec: full FNOL create (typeahead→in-force badge→searchable cause→char-counter→review→create→
  success snackbar+nav); the COMPLETE §4.2 lifecycle (Draft→Open→UnderInvestigation→Open→PendingPayment→
  Closed-w/-justification→Reopened→Open + Withdrawn, with require-reason enforcement); document upload (incl.
  the progress bar on a ~6MB file)+list+download URL; EVERY dashboard filter actually filtering (status
  multi-select, search, cause, date-range, handler autocomplete) + Reset + empty-state + pagination; role-gated
  approve/reject/retract (dynamic on role switch), approve→GL-Posted, ≤$10k auto-approval, **BR-R-03 live**
  (self-approval→422); the **Retry-GL** flow (Failed→re-enqueue→Posted, + idempotent no-op = BR-R-06); reject
  (reason dialog), notes save, add party, audit related-entity tab-jump, skeleton loaders, all 7 status-badge
  colours, and the 422 error path in a snackbar. Only the copy-to-clipboard SUCCESS path was unexercisable
  (Clipboard API blocked in the headless preview; its error branch is verified). `ng build` clean (no budget
  warnings), 12 FE tests pass, no console errors.
  Deliberately NOT changed (documented as reasonable): loss date is day-precision (no time picker; future-date
  validated); FNOL added items are editable bordered rows rather than read-only chips; last-Claimant remove is
  hidden rather than shown-disabled; dashboard "Cause" shows the code; the pre-submit warning confirmation
  covers client-detectable warnings (no-policy / outside-period) while server-only warnings (e.g. no risk
  objects) surface post-create in the success snackbar. NOTE: bringing up the stack required dropping a stale
  container DB so the current `InitialCreate` migration applied from scratch — re-validating §15.4.
- **Aspire**: AppHost wires SQL Server + API + Angular (`AddNpmApp`). Runs in VS 2026 and via `dotnet run`.
- **Azure IaC + CI/CD**: `iac/` Bicep (App Service, Static Web App, Azure SQL, Blob, Key Vault, Log
  Analytics/App Insights) + `azure.yaml` (azd) + `.github/workflows/ci-cd.yml`. `bicep build` clean.
- **Verified end-to-end** locally via WSL against real SQL: FNOL→party→reserve(auto+supervisor-approve)→
  GL posting→audit→status; role-gating; tenant filter; documents; all 422/403/409 paths.
- **NOT done**: actual deploy to a live Azure subscription (needs the user's account). AI interaction
  history: user has exported it.

## Tech stack
.NET 9 / C# 13, ASP.NET Core Web API, MediatR 12, EF Core 9, FluentValidation 11, AutoMapper 13,
Hangfire 1.8, SQL Server 2022, Azure Blob, .NET Aspire 9.5.2, Angular 21 + Material. Clean Architecture.

## Solution layout
```
src/core/ClaimsModule.Domain         entities, enums, domain events, ClaimLifecycle, ReserveAuthority, SequentialGuid
src/core/ClaimsModule.Application    MediatR commands/queries+handlers, validators, DTOs, interfaces, AutoMapper, ValidationBehaviour
src/infra/ClaimsModule.Infrastructure storage (Azure Blob + local), Hangfire scheduler + jobs, SystemDateTime
src/infra/ClaimsModule.Persistence   ClaimsDbContext, EF configs, Migrations, repositories, UnitOfWork, ClaimNumberGenerator, AuditLogService, Seeding
src/api/ClaimsModule.API             controllers, ExceptionHandlingMiddleware, CurrentUserService (mock auth), Program.cs (composition root)
src/aspire/ClaimsModule.AppHost      Aspire orchestration (SQL + API + web)
src/aspire/ClaimsModule.ServiceDefaults  OpenTelemetry/health/resilience
src/clients/web                      Angular app (claims-module-web)
tests/ClaimsModule.Application.Tests / ClaimsModule.Api.IntegrationTests
iac/                                 Bicep (main.bicep + modules/*) ; azure.yaml at root ; .github/workflows/ci-cd.yml
```

## ⚠️ Environment gotchas (CRITICAL — read before building/running)
1. **.NET 9 SDK location.** Machine has SDK 10.0.300 machine-wide + .NET 9 user-local at
   `C:\Users\bohda\.dotnet` (9.0.314). The user has now ALSO installed .NET 9 SDK machine-wide so VS 2026
   resolves `global.json` (pins `9.0.x`, `rollForward: latestMinor`). For CLI builds, prepend the SDK to PATH:
   `$env:DOTNET_ROOT="C:\Users\bohda\.dotnet"; $env:PATH="C:\Users\bohda\.dotnet;$env:PATH"`.
   The `dotnet-ef` tool is installed user-global (9.x).
2. **Smart App Control (SAC) is ENFORCED on Windows.** It blocks *loading freshly-built unsigned .NET
   DLLs at runtime* (`0x800711C7`). So `dotnet build` and unit tests pass, but `dotnet run` / the
   integration test on Windows can fail to load `ClaimsModule.Persistence.dll` after a rebuild. It's
   per-file-hash and inconsistent. **Not fixable** (no folder exclusions; self-signing doesn't satisfy SAC;
   turning SAC off is one-way). CI/Azure are unaffected. **To run locally reliably, use WSL** (below).
   VS 2026 currently runs it OK (machine-wide SDK), but a future rebuild may get SAC-blocked.
3. **PowerShell ↔ WSL quoting is fragile.** `$(...)`, `(`, backslashes, and bash arrays inside
   `wsl -d Ubuntu -u root bash -c "..."` frequently get mangled by PowerShell. **Always put non-trivial
   bash in a script file** (write to `C:\...\Temp\x.sh`, run `wsl ... bash -c "tr -d '\r' < /mnt/c/.../x.sh | bash"`).
4. **`pkill -f dotnet` self-kills** (its own bash cmdline contains "dotnet" → exit 9). Use `pkill -9 dotnet`
   (match by process NAME, not `-f`) — safe; or kill by port via `ss`.

## Build / run
```powershell
# build + test (Windows; build is NOT SAC-blocked)
$env:DOTNET_ROOT="C:\Users\bohda\.dotnet"; $env:PATH="C:\Users\bohda\.dotnet;$env:PATH"
dotnet build ClaimsModule.sln ; dotnet test ClaimsModule.sln

# EF migration
dotnet ef migrations add X --project src/infra/ClaimsModule.Persistence --startup-project src/api/ClaimsModule.API

# Aspire (VS 2026 F5, or CLI — needs Docker). DCP "Failed to subscribe to notifications" = harmless noise.
dotnet run --project src/aspire/ClaimsModule.AppHost
# Frontend standalone: cd src/clients/web ; ng serve   (Node is NOT SAC-blocked)
```

### Running the .NET API locally via WSL (the SAC-proof path)
WSL Ubuntu distro is installed; .NET 9 SDK at `/opt/dotnet` + `libicu-dev` (ICU required — the app uses
`en-US` culture, so invariant mode is NOT an option). Pattern that works (avoids VS file locks on
`/mnt/d/.../bin` by building from a copy in the Linux fs):
- SQL on Windows Docker: `docker run -d --name claims-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=<StrongP@ss1> -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest` (throwaway local-only SA password — pick your own; the cloud DB uses the `AZURE_SQL_ADMIN_PASSWORD` secret / Key Vault, never this)
- From WSL the Windows host SQL is reachable at **`10.255.255.254:1433`**.
- Helper scripts in `%TEMP%`: `run-api.sh` (builds from `/mnt/d`) and `run-api-val.sh` (copies src to
  `/root/claims-val`, builds there — use this when VS holds the `/mnt/d/bin` locks). Both set
  `ASPNETCORE_URLS=http://0.0.0.0:5131` (so Windows port-forwarding exposes it at `localhost:5131`) and a
  `ConnectionStrings__ClaimsDb` pointing at `10.255.255.254,1433`.
- Frontend on Windows: `ng serve` → `proxy.conf.js` falls back to `localhost:5131` → WSL API.
- **Stop the previous WSL API before re-running** (`pkill -9 dotnet`), else port clash / `rm -rf` of a
  running dir → exit 134.

## Key design decisions & DELIBERATE deviations (do NOT "fix" these blindly)
- **GUID PKs are `ValueGeneratedNever()` + client-side COMB GUIDs (`SequentialGuid`)**, NOT
  `NEWSEQUENTIALID()`. This was a deliberate fix: a store-generated key + client-assigned value made EF
  treat children added to a *loaded* aggregate as existing rows → spurious `409 DbUpdateConcurrencyException`
  on reserve/party/document adds. COMB GUIDs keep the §15.1 "sequential GUID" intent. Documented in ARCHITECTURE §10.
- **Program.cs: EF migration runs FIRST (right after `builder.Build()`), before anything constructs the
  Hangfire SQL storage.** Hangfire's `SqlServerObjectsInstaller` installs its schema in the storage ctor;
  if that runs before the DB exists, the install fails and tables are missing (`Invalid object name 'HangFire.Job'`)
  → FNOL create 500s. Keep migration before the middleware/`UseHangfireDashboard`/`PrepareHangfireStorage`.
- **Tenant global query filter** in `ClaimsDbContext.OnModelCreating`: every `ITenantScoped` entity is
  filtered by the single seeded `SeedConstants.OrganisationId` (combined with soft-delete). Single fixed org per §15.1.
- **Reserves are event-sourced** (`ReserveHistory` append-only; balance = sum of effective txns).
- **Domain is MediatR-free**; events wrapped in `DomainEventNotification<T>` at the Application boundary,
  dispatched by `ClaimsDbContext.SaveChangesAsync` **before** base save (audit commits in same transaction).
- **Audit** only via `IAuditLogService` (append-only); GL_POSTING_FAILED handled by `GlPostingFailureStateFilter` (IApplyStateFilter on FailedState).
- **Accepted advisories**: AutoMapper 13.0.1 `NU1903` (last MIT line, DoS not reachable here); OpenTelemetry
  exporter 1.12.0 `NU1902` (transitive via Aspire). Don't bump blindly (15+/Aspire 13 = churn/licensing).
- **CC-02** (no unresolved Critical issues at closure) enforced *preventively* (no validation-issue store).
- **CauseOfLossCodes / ClaimStatusTransitions** intentionally NOT tenant-scoped (global lookups).

## Mock auth (local)
Headers drive identity (`CurrentUserService`): `X-User-Id`, `X-User-Role` (Handler/Supervisor/Manager),
`X-User-Name`. Seeded user GUIDs: Handler `aaaaaaaa-…-001`, Supervisor `bbbb…-002`, Manager `cccc…-003`.
Org is fixed `SeedConstants.OrganisationId` = `11111111-1111-1111-1111-111111111111`. Frontend interceptor
adds these + a fake Bearer. Reserve approve/reject UI gated to Supervisor+.

## Azure deploy (remaining work)
`azd up`, or the GitHub Actions pipeline. Needs repo vars `AZURE_ENV_NAME`/`AZURE_LOCATION` and secrets
`AZURE_CLIENT_ID`/`AZURE_TENANT_ID`/`AZURE_SUBSCRIPTION_ID` (OIDC app reg) + `AZURE_SQL_ADMIN_PASSWORD`.
Frontend prod build needs `apiBaseUrl` = deployed API URL (CI rewrites `environment.ts`); API CORS = SWA origin.

## Known minor gaps (optional polish)
All previously-listed minor gaps are now CLOSED (2026-06): assigned-handler filter on the dashboard
(`ClaimSummaryDto.AssignedHandlerId` + claims-list dropdown/column); GL "Failed" **Retry GL** button +
`POST .../reserves/{txnId}/retry-gl-posting` endpoint (`RetryGlPostingCommand`, domain `RetryGlPosting`,
`GL_POSTING_RETRY_REQUESTED` audit, Supervisor/Manager-gated, idempotent Failed→Pending re-enqueue);
document-type picker + upload-progress bar (single source of truth: Domain `DocumentTypes`, used by the
upload validator AND served by `GET /api/reference/document-types`, which the Angular picker fetches at
runtime — no hardcoded FE list); per-component skeleton loaders (reusable `app-skeleton` on
claims-list + claim-detail). Nothing outstanding here blocks the slice.

## Docs
`ARCHITECTURE.md` (architecture, §9 Azure, §15 CI/CD), `AI-WORKFLOW.md` (AI usage + corrected errors),
`README.md` (setup incl. the WSL/SAC recipe). Reconciliation of requirements↔implementation was done and is accurate.
