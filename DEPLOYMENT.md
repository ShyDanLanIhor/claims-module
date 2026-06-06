# Deployment Runbook — Azure via GitHub Actions

Step-by-step to deploy this slice to Azure using the committed pipeline
(`.github/workflows/ci-cd.yml`) + Bicep (`iac/`). The pipeline **provisions** the infra, **runs EF
Core migrations**, deploys the **API** (App Service) and the **Angular app** (Static Web App), and
rebuilds the frontend against the live API URL. Commands are PowerShell (Windows); the `az` calls are
cross-platform.

> The actual deploy runs under **your** Azure subscription + GitHub account. You sign in interactively
> (`az login`); nothing here stores credentials in the repo.

---

## 0. Prerequisites (install once)

```powershell
winget install Microsoft.AzureCLI      # az
winget install GitHub.cli              # gh (optional but used below)
# then restart the shell so az/gh are on PATH
az login                               # browser sign-in to your Azure account
az account set --subscription "<YOUR_SUBSCRIPTION_ID_OR_NAME>"
gh auth login                          # browser/device sign-in to GitHub
```

Choose values up front:

| Name | Example | Notes |
|------|---------|-------|
| `ENV_NAME` | `claims-prod` | short, lowercase — names the RG (`rg-claims-prod`) + tags resources. Keep ≤ ~12 chars (Azure SQL server name length). |
| `LOCATION` | `westeurope` | any region you have quota in. |
| SQL admin password | `Cl@ims_Prod_2026!` | Azure SQL complexity: ≥ 8 chars, 3 of {upper, lower, digit, symbol}. |
| `OWNER/REPO` | `bohdanshyryi/claims-module` | your GitHub repo. |

---

## 1. Create the GitHub repo and push

`docs/*.docx` (the confidential specs) are git-ignored and will **not** be pushed — verified.

```powershell
# from the repo root: D:\repos\claims-module
gh repo create claims-module --private --source=. --remote=origin --push
# (or, if you created the repo in the web UI:)
# git remote add origin https://github.com/<OWNER>/<REPO>.git
# git push -u origin main
```

Create the `production` **Environment** the deploy job references (Settings → Environments → New
environment → `production`). If you add required reviewers there, the deploy will pause for approval.

---

## 2. Create the Azure AD app + OIDC federated credential + role

The `deploy` job authenticates with **OIDC** (no stored client secret). Because the deploy job runs in
`environment: production`, the federated-credential **subject must be the environment**, not a branch.

```powershell
$SUB_ID    = az account show --query id -o tsv
$TENANT_ID = az account show --query tenantId -o tsv
$APP_ID    = az ad app create --display-name "claims-module-ghactions" --query appId -o tsv
az ad sp create --id $APP_ID

# Federated credential matching the deploy job's environment:
$fed = @{
  name      = "gh-prod-env"
  issuer    = "https://token.actions.githubusercontent.com"
  subject   = "repo:<OWNER>/<REPO>:environment:production"
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json -Compress
az ad app federated-credential create --id $APP_ID --parameters $fed

# Role: Owner at subscription scope.
# Owner (not just Contributor) is required because the Bicep CREATES role assignments
# (the App Service identity gets "Key Vault Secrets User"). Contributor cannot write role assignments.
az role assignment create --assignee $APP_ID --role Owner --scope "/subscriptions/$SUB_ID"
```

> Least-privilege alternative to `Owner`: grant **Contributor** + **User Access Administrator** instead.

---

## 3. Set GitHub repo variables + secrets

```powershell
gh variable set AZURE_ENV_NAME       -b "claims-prod"
gh variable set AZURE_LOCATION       -b "westeurope"

gh secret   set AZURE_CLIENT_ID        -b "$APP_ID"
gh secret   set AZURE_TENANT_ID        -b "$TENANT_ID"
gh secret   set AZURE_SUBSCRIPTION_ID  -b "$SUB_ID"
gh secret   set AZURE_SQL_ADMIN_PASSWORD -b "<your strong SQL password>"
```

(Or via the web UI: Settings → Secrets and variables → Actions → **Variables** / **Secrets**.)

---

## 4. Run the pipeline

The push in Step 1 already triggered a run — but its `deploy` job will have failed because the secrets
didn't exist yet. With secrets now set, start a clean run:

```powershell
gh workflow run ci-cd --ref main
gh run watch                      # live status, or use the Actions tab
```

What it does: build + test → `az deployment sub create` (Bicep) → open a temp SQL firewall rule for the
runner → `dotnet ef database update` → close the firewall rule → publish + deploy the API → rebuild the
Angular app with `apiBaseUrl = <deployed API>/api` → deploy the Static Web App.

---

## 5. Verify

```powershell
# discover the URLs (or read them from the run log / Azure portal)
az webapp list      -g rg-claims-prod --query "[].defaultHostName" -o tsv
az staticwebapp list -g rg-claims-prod --query "[].defaultHostname" -o tsv
```

- **API**: `https://<api-host>/health` → `200`; `https://<api-host>/swagger` loads (served in Production).
- **Frontend**: open the SWA URL → dashboard loads, run an FNOL end-to-end (confirms CORS = SWA origin
  and the FE is pointed at the live API).
- **Azure portal**: `rg-claims-prod` contains App Service + plan, Static Web App, Azure SQL (server + db),
  Storage, Key Vault, Log Analytics + App Insights.

---

## 6. Tear down (avoid ongoing charges)

The B1 App Service plan + Azure SQL bill while they exist. When done:

```powershell
az group delete --name rg-claims-prod --yes --no-wait
```

---

## Gotchas / notes

- **OIDC subject** must match the workflow trigger that authenticates. Here it's
  `…:environment:production` (the `deploy` job's `environment:`). If you later remove that `environment:`,
  switch the subject to `repo:<OWNER>/<REPO>:ref:refs/heads/main`.
- **Owner role** on the SP is needed for the in-Bicep role assignment (Key Vault Secrets User). Without
  it, provisioning fails with `AuthorizationFailed` on `Microsoft.Authorization/roleAssignments/write`.
- **SQL password** must satisfy Azure SQL complexity, and must be identical in the
  `AZURE_SQL_ADMIN_PASSWORD` secret (used both to provision the server and to run migrations).
- **`ENV_NAME`** keep short + lowercase: it forms `rg-<env>` and feeds the resource-name `uniqueString`;
  the Azure SQL logical server name has a 63-char limit.
- **Static Web App region** defaults to `eastus2` (`staticWebAppLocation` in `iac/main.bicep`) — a
  SWA-supported region, independent of `LOCATION`.
- **First run after Step 1** fails at `deploy` (no secrets yet) — expected; re-run after Step 3.
- Migrations run from the GitHub runner (startup migration is intentionally **off** in Production —
  `Database__ApplyMigrationsAtStartup=false` in `iac/modules/api.bicep`).

See `ARCHITECTURE.md` §9 (Azure topology) and §15 (CI/CD) for the design rationale.
