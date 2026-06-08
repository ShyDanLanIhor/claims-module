# Claims Module

A vertical slice of a **Claims Management** module — First Notice of Loss (FNOL) intake and
Reserve Management — for an insurance Policy Administration System. Built as a technical
assessment (DICEUS, Fullstack .NET + Angular).

[![ci-cd](https://github.com/ShyDanLanIhor/claims-module/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/ShyDanLanIhor/claims-module/actions/workflows/ci-cd.yml)

## Live deployment
- **Frontend (Static Web App):** https://jolly-ocean-0db0ca80f.7.azurestaticapps.net
- **API:** https://app-api-we6lhaipoe6g4.azurewebsites.net — [`/swagger`](https://app-api-we6lhaipoe6g4.azurewebsites.net/swagger) · [`/health`](https://app-api-we6lhaipoe6g4.azurewebsites.net/health)

Deployed to Azure (Poland Central) via the GitHub Actions pipeline; every push to `main` re-provisions
(idempotent), applies EF migrations, and redeploys the API + Static Web App.

## Stack
- **Backend:** .NET 9, ASP.NET Core Web API, MediatR (CQRS), EF Core 9, FluentValidation, AutoMapper, Hangfire
- **Orchestration:** .NET Aspire (AppHost + ServiceDefaults)
- **Frontend:** Angular 21 (standalone, zoneless, signals), Angular Material
- **Database:** SQL Server / Azure SQL
- **Cloud:** Microsoft Azure (provisioned via azd / Bicep)
- **CI/CD:** GitHub Actions

## Repository Layout
```
src/
  aspire/      ClaimsModule.AppHost, ClaimsModule.ServiceDefaults
  core/        ClaimsModule.Domain, ClaimsModule.Application
  infra/       ClaimsModule.Infrastructure, ClaimsModule.Persistence
  api/         ClaimsModule.API
  clients/web/ Angular app (claims-module-web)
tests/         Application.Tests, Api.IntegrationTests
iac/           Azure IaC: main.bicep + modules (App Service, Static Web App, Azure SQL, Blob, Key Vault, monitoring)
docs/          Confidential reference documents (local only, gitignored)
```

## Getting Started

**Prerequisites:** .NET 9 SDK (pinned in `global.json`), Node.js 20+, Angular CLI. For the
one-command Aspire run: a container runtime (Docker Desktop / Podman). For running the API
standalone: SQL Server 2022 or LocalDB. EF tooling: `dotnet tool install --global dotnet-ef --version 9.*`.

### Configuration
Key settings live in [`src/api/ClaimsModule.API/appsettings.json`](src/api/ClaimsModule.API/appsettings.json):

| Setting | Example | Notes |
|---|---|---|
| `ConnectionStrings:ClaimsDb` | `Server=(localdb)\\mssqllocaldb;Database=ClaimsModule;Trusted_Connection=True` | SQL Server / Azure SQL |
| `Storage:Provider` | `LocalFileSystem` \| `AzureBlob` | document storage provider |
| `Storage:ConnectionString` | _(blob conn string)_ | required when `Provider = AzureBlob` |
| `Database:ApplyMigrationsAtStartup` | `true` | auto-applies migrations on startup (dev) |

**Mock authentication (no real IdP).** Identity comes from request headers — the frontend sets them
automatically and its role switcher changes them. For manual API calls (Swagger / curl):

| Header | Example | Notes |
|---|---|---|
| `X-User-Id` | `aaaaaaaa-0000-0000-0000-000000000001` | seeded user id |
| `X-User-Role` | `Handler` \| `Supervisor` \| `Manager` | drives authorization (reserve approval, status gates) |
| `X-User-Name` | `Hannah Handler` | display only |

Seeded test users: **Handler** `aaaaaaaa-…-001`, **Supervisor** `bbbbbbbb-…-002`, **Manager** `cccccccc-…-003`
(fixed organisation `11111111-1111-1111-1111-111111111111`).

**Idempotency:** write requests (POST/PUT/PATCH/DELETE) may send an `Idempotency-Key` header — the first
2xx response is recorded per-tenant and replayed on a retry with the same key, so the operation runs once.

### Database
```bash
dotnet ef database update \
  --project src/infra/ClaimsModule.Persistence \
  --startup-project src/api/ClaimsModule.API
```
Reference data (cause-of-loss codes, simulated policies, status transitions, claim-number sequences)
is seeded via EF Core `HasData` and applied by the migration — no manual setup required.

### Run everything with Aspire (recommended)
```bash
cd src/clients/web && npm install && cd -      # one-time, so the dev server can start
dotnet run --project src/aspire/ClaimsModule.AppHost
```
The AppHost provisions a **SQL Server** container, starts the **API** (injecting the DB connection
string and applying migrations on startup) and the **Angular dev server** (:4200), and opens the
Aspire dashboard with logs, traces and health for all three. Requires a running container engine.

### Run the API standalone (no container engine)
```bash
dotnet build
dotnet run --project src/api/ClaimsModule.API     # Swagger at /swagger, Hangfire dashboard at /hangfire
```
Uses `ConnectionStrings:ClaimsDb` from appsettings (LocalDB by default). Apply migrations first
(see Database below).

### Tests
```bash
dotnet test
```

### Frontend
```bash
cd src/clients/web
npm install
ng serve          # http://localhost:4200
```
Angular 21 (standalone, zoneless, signals + Angular Material). The dev build targets the API at
`http://localhost:5131/api` (see `src/environments/environment.development.ts`); the API's CORS already
allows `http://localhost:4200`. Use the header **user menu** to switch between Handler / Supervisor /
Manager roles — reserve approval actions are gated to Supervisor and above. Screens: claims dashboard,
multi-step FNOL intake, and claim detail (Overview / Parties / Reserves / Documents / Audit Log).

> **Note on the SDK:** this environment had only .NET 8 and .NET 10 installed, so a user-local .NET 9
> SDK was installed and pinned in `global.json`. If `dotnet` resolves to a different SDK, ensure a 9.0.x
> SDK is on `PATH`.

### Running locally when Windows Smart App Control (SAC) is enforced
SAC blocks **loading freshly-built, unsigned .NET assemblies at runtime** (`0x800711C7`) — `dotnet build`
and unit tests pass, but `dotnet run` / integration tests can fail to load `*.dll`. SAC is local-only
(CI and Azure are unaffected). The reliable local workaround is to run the .NET API inside **WSL2**
(Linux has no SAC), with SQL on Windows Docker and the Angular dev server on Windows:

```bash
# one-time: a Linux distro + the .NET 9 SDK + ICU
wsl --install -d Ubuntu
wsl -d Ubuntu -u root bash -c "apt-get update && apt-get install -y libicu-dev"
wsl -d Ubuntu -u root bash -c "curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir /opt/dotnet"

# 1) SQL Server on Windows Docker
docker run -d --name claims-sql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=<StrongP@ss1>" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# 2) API inside WSL (reaches Windows SQL at 10.255.255.254; binds 0.0.0.0 so Windows can forward to it)
wsl -d Ubuntu -u root bash -lc '
  export DOTNET_ROOT=/opt/dotnet PATH=/opt/dotnet:$PATH ASPNETCORE_URLS=http://0.0.0.0:5131 \
         ConnectionStrings__ClaimsDb="Server=10.255.255.254,1433;Database=ClaimsModule;User Id=sa;Password=<StrongP@ss1>;TrustServerCertificate=True;Encrypt=False";
  cd /mnt/d/repos/claims-module && dotnet run --project src/api/ClaimsModule.API --no-launch-profile'

# 3) Frontend on Windows (proxy.conf.js falls back to localhost:5131 → the WSL API via port-forwarding)
cd src/clients/web && ng serve
```
Then open `http://localhost:4200`. (Alternatively, turn SAC off — Settings → Windows Security → App & browser
control → Smart App Control → **Off** — but that's a one-way change requiring a clean reinstall to undo.)

## Azure deployment
Infrastructure is defined as Bicep under [`iac/`](iac/) (azd-compatible via [`azure.yaml`](azure.yaml)) and
provisions: **App Service** (API), **Static Web App** (frontend), **Azure SQL**, **Blob Storage**,
**Key Vault**, and **Log Analytics + Application Insights**. Connection strings live in Key Vault and are
consumed by the API via managed-identity Key Vault references (no secrets in config). See ARCHITECTURE.md §9.

**Option A — one command with the Azure Developer CLI:**
```bash
azd auth login
azd up        # prompts for environment name, location, and the SQL admin password
```

**Option B — CI/CD (GitHub Actions, [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml)):**
build & test backend → build frontend → provision (Bicep) → run EF migrations → deploy API + Static Web App.
Configure once, then run the workflow (manual `Run workflow` or push to `main`):
- Repository **variables**: `AZURE_ENV_NAME`, `AZURE_LOCATION` (e.g. `westeurope`)
- Repository **secrets**: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (an OIDC federated
  credential / app registration), and `AZURE_SQL_ADMIN_PASSWORD`.

> The IaC and pipeline are deploy-ready and validated (`bicep build` clean), but have not been run against a
> live subscription here — deploying requires your Azure account.

## Documentation
- [ARCHITECTURE.md](ARCHITECTURE.md) — architecture overview, layering, and design decisions.
- [AI-WORKFLOW.md](AI-WORKFLOW.md) — how AI tooling was used during development.
