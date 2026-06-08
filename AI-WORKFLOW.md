# AI Workflow Report

## 1. AI Tools Used
- **Claude (chat)** — architecture and repository-structure reasoning, planning, documentation drafting.
- **Claude Code (Opus 4.8)** — agentic implementation in the repository: scaffolding, code generation,
  EF migration generation, running builds/tests, multi-agent reconciliation/review workflows, and
  iterative correction. No other AI coding tools were used.

## 2. Workflow Approach
Phased delivery:
1. **Read the specification carefully.** Both assessment documents (`.docx`) were extracted and read in
   full before any implementation, and the existing skeleton/architecture notes were checked against them.
2. **Reason through architecture** with Claude in chat (layering, aggregate boundaries, event flow).
3. **Scaffold** the solution and Angular workspace with Claude Code (separate earlier step).
4. **Implement each layer inside-out** (Domain → Application → Persistence → Infrastructure → API),
   *building after every layer* so errors were caught immediately rather than at the end.
5. **Generate the EF Core migration**, then add unit + integration tests and run the full suite.
6. **Adversarially review** the business-rule implementation against the FRS, then draft the docs.
7. **Architecture-quality pass** (after the spec was satisfied): a separate review for design problems
   whose fix would not contradict the spec — each candidate finding raised by one agent and then
   independently re-checked by a skeptic agent (real? spec-safe? worth it?), so subjective findings could
   not survive on assertion alone. Confirmed findings were fixed in phases and re-verified spec-safe.

Context-loading strategy: the full functional specification was loaded up front; each layer was
implemented with the relevant FRS sections (business rules §7, validation §8, entities §9, conventions
§15) kept in context so generated logic stayed domain-correct. Business-rule identifiers (BR-C-*, BR-R-*)
were referenced directly in code comments to keep the implementation traceable to the spec.

## 3. Representative Prompts
1. **Repository scaffolding** — "Set up the .NET 9 Clean Architecture solution and Angular workspace
   with the agreed folder structure; do not implement business logic yet." (produced the skeleton)
2. **Backend vertical slice** — "Read the two `.docx` specs, verify what's appropriate at this stage,
   then start implementing, and — analysing our conversation — fill in `ARCHITECTURE.md` and
   `AI-WORKFLOW.md`." This drove the full Domain→API implementation, the migration, the tests, and these
   documents, with the explicit constraint to *keep the solution building*.
3. **Domain-rule encoding** (implicit in the above) — translate FRS §6–§8 into a domain that enforces
   the three-tier reserve authority, the self-approval ban (BR-R-03), the $10M aggregate override
   (BR-R-05), event-sourced reserves, the lifecycle state machine (§4.2) and Hangfire idempotency (§12.1).
4. **Verified reconciliation & review** — "Do a critical, complete reconciliation of the requirements vs
   what's implemented." Run as a multi-agent workflow: parallel sub-agents each audited one requirement
   area (data model, API, business rules, jobs, frontend, Azure, deliverables) against the spec docs and
   the actual code, returning structured findings; an adversarial pass then reviewed the resulting diff.
   This surfaced real gaps a single-pass review had missed (Swagger/health gated to Development, missing
   FNOL fields, read-only Claim-Detail tabs) and were then fixed and re-verified.

## 4. AI-Generated vs. Manually Designed
- **AI-generated:** project scaffolding, entity/DTO/validator/handler boilerplate, EF Core
  configurations, the migration, repositories, controllers, and the first drafts of these documents.
- **Human-directed / reviewed:** the architectural decisions (keeping the Domain free of MediatR,
  dispatch-before-save for atomic audit, the read/write repository split, event-sourced reserves),
  the interpretation of ambiguous business rules against the FRS, the dependency-advisory decision, and
  the acceptance criteria for "done" (build green + migration + tests passing).

## 5. Examples of AI Errors Corrected
**During architecture planning:**
1. *Self-introduced naming collision.* While discussing folder names, the AI added a root `infra/`
   folder for Azure IaC, then used that folder's existence as the reason to reject naming the Clean
   Architecture infrastructure layer `src/infra/`. The reasoning was circular — it had created the
   conflict itself. Resolved by keeping `infra/` for the code layer and naming the cloud IaC folder `iac/`.
2. *Misread folder placement.* When asked for the frontend at `src/clients/web/`, the AI initially read
   `clients/` as a top-level folder and moved the Angular app out of `src/`, contradicting the decision
   that `src/` holds all shipping source. Corrected; the frontend stays at `src/clients/web/`.

**During implementation:**
3. *Wrong environment assumption surfaced.* The task assumed a .NET 9 SDK was installed; the tooling
   check found only 8.0 and 10.0.300. Rather than silently building on .NET 10, this was surfaced and a
   user-local .NET 9 SDK was installed and pinned via `global.json`.
4. *NuGet package downgrade (NU1605).* The Infrastructure project initially pinned
   `Microsoft.Extensions.*` to `9.0.*`, which conflicted with `Azure.Core`'s transitive requirement of
   `10.0.3` and failed the build. Fixed by aligning those abstractions to `10.0.*` (TFM-agnostic, safe on
   net9) and switching to `Microsoft.Extensions.Options.ConfigurationExtensions`.
5. *Integration test enum (de)serialisation.* The first integration test used default `System.Text.Json`
   options and failed to deserialise the API's string-serialised enums. The test run caught it; fixed by
   adding `JsonStringEnumConverter` to the test's deserialiser — confirming the API behaviour was correct
   and the test was wrong.
6. *Security-advisory judgment.* The AI selected `AutoMapper 13.0.1`, which raised a high-severity NuGet
   advisory (GHSA-rvv3-g6hj-g44x). Instead of blindly bumping into the commercially-licensed 15+ line, the
   advisory was assessed (a DoS not reachable in this design) and documented with rationale and an upgrade
   path — see ARCHITECTURE.md §10.

**Found only by actually running the stack** (caught during a live end-to-end run, not by the build):
7. *EF concurrency regression from a client-set store-generated key.* An earlier fix made entity `Id`s
   client-side sequential (COMB) GUIDs while the keys were still configured store-generated
   (`NEWSEQUENTIALID`). EF then treated any child added to an *already-loaded* aggregate (reserve, party,
   document) as an existing row → `UPDATE` → 0 rows → spurious `409 DbUpdateConcurrencyException`. FNOL
   create escaped it only because it `Add`s the whole graph. Fixed by marking GUID keys
   `ValueGeneratedNever()` (the app always supplies the value); migration regenerated.
8. *Hangfire schema startup race.* FNOL create returned `500 Invalid object name 'HangFire.Job'` when the
   first job was enqueued before the Hangfire server finished creating its SQL schema. Fixed by preparing
   Hangfire storage synchronously at startup before serving requests.
Both were reproduced live, root-caused from the server logs, fixed, and re-verified end-to-end.

**Found by adversarial multi-agent reconciliation (auditing requirements against the code):**
9. *Swagger & the `/health` probe gated to Development only.* The deployed App Service runs as Production,
   but Swagger and the `/health` endpoint were mapped only under `IsDevelopment()` — so the live
   `/swagger` would 404 (an explicit §4.3 deliverable) and Azure health probes would mark the instance
   unhealthy and pull it from rotation. Both were changed to map unconditionally.
10. *Rejected an AI reviewer's own false claim.* An audit sub-agent asserted `VALIDATION_ISSUE_ADDED` is
    "never emitted"; verifying against `CreateClaim` (and an earlier live run) showed it IS written for
    every FNOL warning. The finding was discarded — AI output is verified against the code, not trusted,
    even when it originates from another AI agent.

**From a principled deviation review (human-led — "is each documented tradeoff genuinely a *better*
approach, or just a shortcut? If a shortcut, fix it"):**
11. *A documented rationale that was actually false.* ARCHITECTURE §10 claimed `ReserveHistory.PostingJobId`
    couldn't hold the real Hangfire job id without "leaking `PerformContext`". On review that was wrong —
    `IBackgroundJobScheduler.Enqueue(...)` returns the id cleanly — so the code now stores the real id
    (operator dashboard traceability + literal FRS §12.1) and the doc was corrected. AI-written
    *justifications* get audited too, not just AI-written code.
12. *Scope-cuts dressed up as "tradeoffs".* Three §10 items (synthetic posting id, missing `BIT DEFAULT 0`,
    no HTTP `Idempotency-Key`) were not genuinely "better" — they were cuts, so they were implemented
    properly (a global `IdempotencyFilter` + per-tenant response store; DB-level bool defaults). Only
    choices that are genuinely superior (event-sourced reserve "adjust" as a typed POST, not a PUT) were
    kept as documented deviations.
13. *An unnecessary extra migration.* The AI added a standalone `AddIdempotencyAndBoolDefaults` migration;
    the human reviewer pointed out that, with nothing yet deployed, it belongs in `InitialCreate` (a fresh
    clone should build the whole schema in one step). The migrations were squashed into a single
    `InitialCreate`, then verified by applying it to a brand-new database.

**From the architecture-quality review (every finding double-checked by an adversarial skeptic agent):**
14. *A "critical" finding that was simply wrong.* An agent claimed the app would crash at bootstrap
    (NG0908, "Angular requires Zone.js") because neither zone.js nor `provideZonelessChangeDetection()` was
    configured. The skeptic checked the installed `@angular/core` 21 runtime: zoneless is the *default* in
    v20+ (the scheduler is prepended unconditionally) and that error no longer exists — so the "critical"
    was rejected. Of 24 findings raised, 19 were confirmed real *and* spec-safe and fixed; 5 were rejected
    — incl. a proposed `Money` value object that would have *violated* the mandated `DECIMAL(19,4)`, and a
    reserve sign-asymmetry (authority by magnitude vs aggregate ceiling by signed total) that was intended.
15. *Two AI claims about its own code, both false.* One agent said the GL idempotency key had "no unique DB
    index" — the code had one (`ReserveHistoryConfiguration`). Another called the BR-R-05 hard-422 a
    "divergence" — reading the handlers showed it is the correct realization (the block fires only on the
    auto-approval path; pending reserves are gated at approval). Both discarded after checking the source.
16. *Tooling errors caught by the build loop.* The new AutoMapper-validation test first used a 2-arg
    `MapperConfiguration` ctor (the pinned 13.0.1 exposes only the 1-arg form) and a `BuildServiceProvider`
    whose container package the test project doesn't reference; both were surfaced by the very next build
    and fixed — again showing why every change is followed by a build/test run.

**From a live UI walkthrough (driving the running client in a real browser):**
17. *Audit log went stale after in-session actions — caught only by running it.* With the full stack up
    (API via WSL, `ng serve`) and seeded data, the Angular client was driven through a browser. Approving a
    pending reserve correctly refreshed the Reserves tab, but the Audit Log tab still showed the pre-action
    snapshot (3 entries) while the API had already written `RESERVE_APPROVED` and `GL_POSTING_SIMULATED`
    (5 entries). The component's `reloadReserves()` (and the document-upload path) re-fetched reserves and the
    claim but not the audit list. Both now reload the audit log; re-verified live. A static read of §11 could
    not surface this — only exercising the app did.
18. *In-force badge never updated when the loss date changed.* A second, deeper live pass (prompted by the
    question "did you really check everything you could?") drove the FNOL form end-to-end. The Step-1 in-force
    badge (green if the loss date is within the policy period) never appeared: its `computed()` read the loss
    date straight off the reactive form control (`step1.controls.lossDate.value`) — a non-signal read — so it
    only re-evaluated when the *policy* signal changed, not when the *date* changed. In the natural flow (pick
    policy, then enter date) the badge stayed blank. Fixed by mirroring the date into a `signal` (the same
    pattern the component already used for the reserve amount) and reading that in the computed; verified live
    (badge turns green on date entry). Another bug only a real run could reveal — a static read sees a
    `computed()` and assumes reactivity.
19. *A claim with open reserves could never be closed through the UI.* A third, exhaustive pass (prompted by
    "everything needs to be tested") drove the full §4.2 lifecycle. CC-04 requires a justification note to
    close a claim that still has open reserves — but the Closed confirmation dialog rendered its reason field
    only `@if (data.requireReason)`, and `transition()` set `requireReason=false` for Closed. So the dialog
    offered no way to enter the justification, every close attempt returned 422, and the claim was un-closable
    from the UI. Fixed by computing `hasOpenReserves` from `this.reserves()` and requiring (hence showing) the
    justification field when closing with open reserves; verified live (claim with a $53k open reserve now
    closes with a justification). Found only by walking the entire state machine end-to-end.

That third pass exercised the WHOLE client in the browser and confirmed, working per spec: full FNOL create
(typeahead → in-force badge → searchable cause → char-counter → review summary → create → success snackbar +
navigation); the complete §4.2 lifecycle (Draft→Open→UnderInvestigation→Open→PendingPayment→Closed-with-
justification→Reopened→Open, plus Withdrawn — each with the right valid-next-status menu and require-reason
enforcement); document upload (incl. the progress bar on a ~6 MB file) + list + SAS/local download URL;
**every** dashboard filter actually filtering (status multi-select, search, cause dropdown, date-range,
assigned-handler autocomplete) + Reset + the empty-state message + pagination (page 1→2, range/count); reserve
auto-approval and manual approve→GL-Posted; **BR-R-03 live** (self-approval rejected with "Self-approval is not
permitted."); reject (reason dialog), retract; the **Retry-GL** flow on a genuinely-Failed posting (Failed →
re-enqueue → Posted, plus the idempotent no-op when a GL audit already exists = BR-R-06); editable notes save;
add party; the audit related-entity tab-jump; skeleton loaders on load; all seven status-badge colours;
role-switch-driven button gating; and the 422 error path. The one thing not exercisable here was the
copy-to-clipboard *success* path — the Clipboard API is blocked in the headless preview (its error branch is
verified). An adversarial §11 code review separately turned up seven low-severity fidelity gaps (date-future
validation message, FNOL review-summary completeness, inline server errors, party-role badge, Add-Reserve
slide-in panel, snackbar severity), all fixed. Bringing the stack up also re-validated §15.4 (a stale container
DB had to be dropped so the single `InitialCreate` migration rebuilt the schema from scratch — which it did
cleanly).

**From the live Azure deployment (found only by actually deploying through the CI/CD pipeline):**
20. *Integration tests needed SQL the runner didn't have.* The first green-build run failed its integration
    tests on the Linux runner with `PlatformNotSupportedException: LocalDB is not supported`. The test factory
    disabled the Hangfire server + startup migration, but `app.UseHangfireDashboard()` was mapped
    unconditionally — and mapping it eagerly resolves `JobStorage`, constructing the SQL Server storage and
    opening a connection. Locally the tests passed only because a SQL container happened to be reachable. Fixed
    by gating the dashboard behind the same `EnableServer` flag; verified 9/9 integration tests then pass with
    no SQL at all — and the CI gate is now genuinely infrastructure-free.
21. *Trial-subscription SQL region gating.* Provisioning failed with `RegionDoesNotAllowProvisioning` for new
    SQL servers — not only in West Europe but also North Europe, East US 2 and West US 2 (broadly gated for
    free-trial subscriptions). Instead of guessing one region per ~10-minute pipeline run, a quick
    `az sql server create` probe across regions found the working one — **Poland Central** (also the closest
    Azure region to Ukraine). Only a variable change; no code touched.
22. *ARM mangles deployment-output casing.* The deploy step read Bicep outputs via
    `jq -r .AZURE_RESOURCE_GROUP.value`, but ARM returns the keys re-cased (`azurE_RESOURCE_GROUP`,
    `apI_BASE_URL`, …), so every lookup returned `null` and the migration step ran against resource group
    "null". Fixed by lowercasing all output keys (`jq 'with_entries(.key |= ascii_downcase)'`) before reading,
    plus a rerun-safe deployment name (`run_id`+`run_attempt`).
None of these three were code-logic defects — they were infrastructure/CI realities that only a real
deployment surfaces (the earlier WSL+SQL runs masked #20, and #21/#22 are Azure-specific). After the fixes the
pipeline went green end-to-end, and a live FNOL create on Azure returned `201` with `CLM-2026-0000001`, an
auto-approved reserve **posted to the simulated GL by the in-Azure Hangfire server**, and the full audit trail
written — confirming the deployed stack (App Service + Azure SQL via Key Vault + Hangfire + Static Web App with
CORS to the SWA origin) works exactly as designed.

**From driving the deployed UI in a real browser (found only by testing live production):**
23. *Deep-links / refresh 404'd on the Static Web App.* Clicking through from `/` worked, but navigating
    directly to — or refreshing on — any Angular route (`/claims`, `/claims/{id}`) returned the SWA's
    "404: Not found" page. Cause: no `staticwebapp.config.json`, so the SWA served paths literally instead of
    falling back to the SPA's `index.html`; in-app router navigation hid the problem because it never hits the
    server. Fixed by adding the config with `navigationFallback` → `/index.html`. The first attempt then
    *failed the SWA deploy's own validation* — the exclude globs used `**` and SWA permits at most one `*` per
    pattern — so it was corrected to single-wildcard patterns. Verified live: direct loads and refreshes of
    `/claims` and `/claims/{id}` now render the app. This fix itself went through the PR-gated pipeline
    (PR → required "Build & test" check → squash-merge → deploy), exercising branch protection on a real
    change. A pure SPA-routing/hosting bug that unit/integration tests — and even the API smoke-test — cannot
    catch; only loading the deployed front-end in a browser does.

## 6. Honest Assessment
AI was most effective for boilerplate and mechanical breadth: entity/DTO/configuration/handler
generation across many files, the EF migration, and first-draft documentation. It was also a strong
reasoning partner for architecture. But it required active correction — it confidently produced a
package-version conflict and a wrong test assumption, and it makes environment assumptions that must be
verified. The highest-leverage human contributions were: insisting on a build-after-every-layer loop
(so failures were local and cheap), validating generated business logic against the FRS rather than
trusting it, and making the judgment calls AI should not make unilaterally (security advisories,
licensing, ambiguous-rule interpretation). The combination — AI for breadth and speed, human for
correctness and judgment — produced more, faster, without surrendering ownership of the result.

In the later stages, requirement-vs-code reconciliation was run repeatedly as parallel sub-agent audits and
iterated until findings stopped appearing; every non-trivial finding was verified against the source before
acting. The rule applied during review was to either fix a deviation or document it honestly, rather than
label a shortcut as a deliberate "better" tradeoff. A final architecture-quality pass (19 fixes spanning
layering, DRY of an ordering-sensitive GL-posting step, domain encapsulation, DB-level GL idempotency, a
live frontend audit-pagination bug, and the global error-handling path) was run the same way — each fix
checked to be spec-safe by an independent adversarial pass before it was accepted.

## 7. AI Interaction History
The complete Claude (chat) and Claude Code interaction history has been exported and is included with the
submission package. The Claude Code session transcripts cover scaffolding, the inside-out implementation,
the multi-agent requirements reconciliation, and the adversarial review/fix loop described above.
