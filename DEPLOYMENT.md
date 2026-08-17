# Deployment Guide — The Acting Collective

This document describes how to deploy the Acting Collective Blazor Server application to production Azure infrastructure, with Cloudflare handling DNS and GitHub Actions handling CI/CD.

## Architecture Overview

```
Cloudflare DNS (theactingcollective.com)
		│  CNAME / A record
		▼
Azure App Service (Linux, .NET 10)  ──uses──▶  Azure Storage Account (Table Storage)
		│                                            (registrations, waitlist tables)
		└──uses──▶ Azure Communication Services (Email)
```

The app is a single ASP.NET Core Blazor Server project (`theactingcollective.csproj`). It has no separate API or database server — state lives in Azure Table Storage, and transactional email goes through Azure Communication Services (ACS).

---

## 1. Azure App Service

1. **Create the App Service Plan and Web App**
   - SKU: Start with **B1 (Basic)** or **P0v3** for production; Blazor Server requires a plan that supports **WebSockets** (all standard plans do) and **Always On** (to keep the SignalR circuit warm).
   - Runtime stack: **.NET 10 (LTS or STS as available)**, Linux or Windows (Linux is cheaper).
   - Region: choose the region closest to your primary audience (e.g., `South Central US` for Mobile, AL).

2. **Enable required App Service settings**
   - **Always On** → On (required so idle circuits don't get recycled and drop SignalR connections).
   - **WebSockets** → On (required for Blazor Server's SignalR transport).
   - **HTTPS Only** → On.
   - **Minimum TLS Version** → 1.2.

3. **Configure Application Settings (environment variables)**
   All secrets are injected here — **never** committed to `appsettings.json`. See the [Production Configuration Checklist](PRODUCTION-CHECKLIST.md) for the full list of keys (`AzureAd__ClientId`, `ConnectionStrings__TableStorage`, `Acs__ConnectionString`, etc.). ASP.NET Core automatically maps `Section__Key` App Service settings to `IConfiguration["Section:Key"]`.

4. **Deployment slots (optional but recommended)**
   - Create a `staging` slot for zero-downtime deploys and smoke testing before swapping into production.

---

## 1a. Microsoft Entra ID Authentication (App Service deployment)

This app authenticates staff **in-code** using `Microsoft.Identity.Web`, not App Service's built-in "Authentication" (Easy Auth) platform feature. Do **not** enable App Service Easy Auth for this app — it would conflict with the OpenID Connect middleware already configured in `Program.cs`. Instead, follow these steps:

1. **Register the app in Microsoft Entra ID**
   - Azure Portal → Microsoft Entra ID → App registrations → New registration.
   - Name: `The Acting Collective` (or similar).
   - Supported account types: **Single tenant** (only your organization's staff should sign in) unless you have a specific multi-tenant need.
   - Redirect URI (Web platform): `https://<your-domain>/signin-oidc`
   - After creation, also add the **front-channel logout URL**: `https://<your-domain>/signout-callback-oidc`

2. **Create a client secret**
   - App registration → Certificates & secrets → New client secret.
   - Copy the secret value immediately (it's only shown once) — this becomes `AzureAd:ClientSecret`.
   - Set an expiration reminder; secrets expire and must be rotated (rotating requires updating the App Service setting, not code).

3. **Record the identifiers you need**
   - **Application (client) ID** → `AzureAd:ClientId`
   - **Directory (tenant) ID** → `AzureAd:TenantId`
   - **Domain** (e.g., `yourtenant.onmicrosoft.com`) → `AzureAd:Domain`

4. **Set App Service Application Settings** (Configuration → Application settings), using the `Section__Key` double-underscore convention so ASP.NET Core maps them to `IConfiguration["Section:Key"]`:

   | App Service Setting Key      | Value                                          |
   |------------------------------|-------------------------------------------------|
   | `AzureAd__Instance`          | `https://login.microsoftonline.com/`            |
   | `AzureAd__Domain`            | Your tenant domain                              |
   | `AzureAd__TenantId`          | Directory (tenant) ID                           |
   | `AzureAd__ClientId`          | Application (client) ID                         |
   | `AzureAd__ClientSecret`      | Client secret value (**mark as a secret**)      |
   | `AzureAd__CallbackPath`      | `/signin-oidc` (already the code default)       |
   | `AzureAd__SignedOutCallbackPath` | `/signout-callback-oidc` (already the code default) |

   These override the empty placeholders committed in `appsettings.json` — the app reads configuration from environment variables at runtime, so nothing needs to change in source to go from dev to prod.

5. **Verify redirect URIs match your production domain exactly**
   - If Cloudflare/App Service custom domain is `https://theactingcollective.com`, the redirect URI registered in Entra ID must be `https://theactingcollective.com/signin-oidc` — mismatches cause `AADSTS50011` errors at sign-in.
   - If you use a staging slot, register its redirect URI too (e.g., `https://<app-name>-staging.azurewebsites.net/signin-oidc`), or sign-in will fail when smoke-testing the slot before swap.

6. **Confirm HTTPS is enforced**
   - OpenID Connect requires HTTPS redirect URIs in production. Ensure **HTTPS Only** is enabled on the App Service (see section 1, step 2) — Entra ID will reject plain-HTTP redirect URIs for production app registrations.

7. **Post-deploy verification**
   - Visit `/staff` while signed out → should redirect to the "Staff Authorization Required" screen with a working "Sign In with Microsoft Entra ID" link.
   - Complete sign-in → should land back on `/staff` with the dashboard visible.
   - Visit `/` and `/check-in` while signed out → both should load normally with no redirect.

---

## 2. Azure Storage Account

The app uses **Azure Table Storage** (via `Azure.Data.Tables`) for two tables: `registrations` and `waitlist`.

1. Create a **Storage Account** (Standard, LRS is sufficient for an MVP; upgrade to ZRS/GRS as traffic grows).
2. No need to pre-create tables — `AzureTableRegistrationStore` calls `CreateIfNotExists()` on startup.
3. Retrieve the **connection string** (Access keys blade) and store it as the `ConnectionStrings__TableStorage` App Service setting.
4. **Recommended hardening**: rotate access keys periodically, or migrate to **Managed Identity + Azure AD authentication for Table Storage** (`TableServiceClient` supports `DefaultAzureCredential`) to avoid connection-string secrets entirely.

---

## 3. Azure Communication Services (Email)

Used by `AcsEmailSender` to send registration confirmation emails.

1. Create an **Azure Communication Services** resource.
2. Provision an **Email Communication Service** and connect/verify a custom domain (e.g., `mail.theactingcollective.com`) — this requires adding SPF/DKIM/DMARC DNS records (see Cloudflare section below).
3. Grab the ACS **connection string** → App Service setting `Acs__ConnectionString`.
4. Set the verified **sender address** (e.g., `noreply@mail.theactingcollective.com`) → App Service setting `Acs__SenderAddress`.

---

## 4. Cloudflare DNS

1. Add an **A** or **CNAME** record pointing your apex/subdomain (e.g., `theactingcollective.com` or `www`) to the Azure App Service's default hostname (`<app-name>.azurewebsites.net`) or its custom domain verification.
2. Add the **custom domain** in Azure App Service → Custom Domains, and complete domain verification (TXT record) and the CNAME/A mapping in Cloudflare.
3. **SSL/TLS mode**: set to **Full (strict)** in Cloudflare since App Service serves valid TLS certificates (use App Service Managed Certificate, free).
4. **Email domain records** (for ACS): add the SPF, DKIM, and DMARC TXT/CNAME records provided by Azure Communication Services domain verification.
5. Consider setting Cloudflare proxy (orange cloud) **off** initially while verifying, then re-enable once DNS propagates and TLS validates.

---

## 5. GitHub Actions Deployment

Add a workflow at `.github/workflows/deploy.yml` that builds and deploys on push to `main`:

```yaml
name: Deploy to Azure App Service

on:
  push:
	branches: [ main ]

jobs:
  build-and-deploy:
	runs-on: ubuntu-latest
	steps:
	  - uses: actions/checkout@v4

	  - name: Setup .NET
		uses: actions/setup-dotnet@v4
		with:
		  dotnet-version: '10.0.x'

	  - name: Restore
		run: dotnet restore theactingcollective/theactingcollective.csproj

	  - name: Build
		run: dotnet build theactingcollective/theactingcollective.csproj -c Release --no-restore

	  - name: Publish
		run: dotnet publish theactingcollective/theactingcollective.csproj -c Release -o ${{ github.workspace }}/publish --no-build

	  - name: Deploy to Azure App Service
		uses: azure/webapps-deploy@v3
		with:
		  app-name: '<your-app-service-name>'
		  publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
		  package: ${{ github.workspace }}/publish
```

**Secret required in GitHub**: `AZURE_WEBAPP_PUBLISH_PROFILE` — download from Azure Portal (App Service → Get publish profile) and store it under **Repository Settings → Secrets and variables → Actions**. Never commit this file.

For higher security, prefer **OIDC federated credentials** (`azure/login@v2` with `permissions: id-token: write`) over publish profiles, avoiding long-lived secrets entirely.

---

## Post-Deployment Smoke Test

1. Visit `/` — confirm the public landing page and registration form load without authentication.
2. Visit `/check-in` — confirm check-in flow works without authentication.
3. Visit `/staff` — confirm you are redirected to Microsoft Entra ID sign-in, and that after signing in with an authorized account you see the dashboard.
4. Submit a test registration and confirm a confirmation email arrives via ACS.
5. Toggle Paid/Unpaid and Check-In on the Staff Dashboard and confirm the change persists (reload the page).
