# Production Configuration Checklist — The Acting Collective

Use this checklist before every production deployment/go-live. Check items off as you configure them in **Azure App Service → Configuration**, not in source control.

## 1. Required Azure Resources

- [ ] Azure App Service (Linux, .NET 10, Always On enabled, WebSockets enabled)
- [ ] Azure Storage Account (Table Storage: `registrations`, `waitlist`)
- [ ] Azure Communication Services resource + Email Communication Service with verified custom domain
- [ ] Microsoft Entra ID App Registration (for staff sign-in)
- [ ] Cloudflare-managed DNS zone for the production domain

## 2. Microsoft Entra ID App Registration

- [ ] App registration created in the Entra ID tenant that staff sign in with
- [ ] Redirect URI added: `https://<your-domain>/signin-oidc`
- [ ] Front-channel logout URL added: `https://<your-domain>/signout-callback-oidc`
- [ ] Client secret (or certificate) generated and stored — **not** in appsettings
- [ ] Decide whether to add App Roles (e.g., "Staff") and update the `RequireStaffRole` policy to `RequireRole("Staff")` instead of just `RequireAuthenticatedUser()` for tighter access control (currently any authenticated tenant user can reach `/staff` — see Risks below)

## 3. App Service Application Settings (Environment Variables)

Set these under **App Service → Configuration → Application settings**. ASP.NET Core maps `Section__Key` to configuration key `Section:Key` automatically.

| Setting (App Service key)             | Source                                             |
|----------------------------------------|-----------------------------------------------------|
| `AzureAd__Instance`                    | `https://login.microsoftonline.com/`               |
| `AzureAd__Domain`                      | Your Entra ID tenant domain                         |
| `AzureAd__TenantId`                    | Entra ID tenant ID                                  |
| `AzureAd__ClientId`                    | App registration Application (client) ID            |
| `AzureAd__ClientSecret`                | App registration client secret (**secret**)         |
| `ConnectionStrings__TableStorage`      | Storage Account connection string (**secret**)      |
| `Acs__ConnectionString`                | ACS connection string (**secret**)                  |
| `Acs__SenderAddress`                   | Verified ACS sender email address                   |
| `Event__RegistrationDeadline`          | Class registration cutoff (ISO 8601 with offset)    |
| `Event__SquarePaymentLink`             | Square checkout link shown to registrants           |
| `Event__ClassDetails`                  | Human-readable class date/time/location string      |
| `ASPNETCORE_ENVIRONMENT`               | `Production`                                        |

- [ ] All secret-bearing settings above are marked as **Deployment slot settings = false** unless intentionally slot-specific
- [ ] Confirm `appsettings.json` / `appsettings.Development.json` still contain only empty placeholders for every key above (see Section 6)

## 4. Connection Strings

- [ ] `ConnectionStrings:TableStorage` set in App Service, **not** `UseDevelopmentStorage=true` (that value is dev-only, pointing at the local Azurite emulator)
- [ ] Storage account access key rotated from any value used during development/testing
- [ ] (Optional hardening) Migrate to Managed Identity-based `TableServiceClient` auth to eliminate the connection-string secret entirely

## 5. DNS Records (Cloudflare)

- [ ] `A`/`CNAME` record for the production hostname → Azure App Service custom domain
- [ ] `TXT` record for Azure App Service domain verification
- [ ] SSL/TLS mode set to **Full (strict)**
- [ ] `TXT`/`CNAME` records for ACS domain verification (SPF, DKIM)
- [ ] `TXT` record for DMARC policy on the email sending domain

## 6. Email Domain (Azure Communication Services)

- [ ] Custom email domain added and **verified** in ACS (not using the default `azurecomm.net` test domain for production)
- [ ] SPF record includes ACS's sending infrastructure
- [ ] DKIM records published and verified
- [ ] DMARC policy published (start with `p=none` for monitoring, tighten to `quarantine`/`reject` later)
- [ ] `Acs__SenderAddress` uses the verified domain (e.g., `noreply@mail.theactingcollective.com`)

## 7. Storage Account

- [ ] Storage account created in the same region as the App Service (reduces latency/egress cost)
- [ ] Redundancy level chosen deliberately (LRS for MVP cost savings; GRS/ZRS if uptime requirements increase)
- [ ] Access keys rotated before go-live (dev/test keys should not carry into production)
- [ ] Firewall/network rules reviewed (allow Azure services, restrict public access if feasible)

## 8. Secrets Hygiene

- [ ] `appsettings.json` and `appsettings.Development.json` contain **no real secrets** — verified empty/placeholder values for `AzureAd`, `ConnectionStrings`, and `Acs` sections
- [ ] Local development secrets are stored via **.NET User Secrets** (`dotnet user-secrets`), keyed to the `UserSecretsId` already present in the `.csproj`
- [ ] Production secrets live **only** in Azure App Service Configuration (or Azure Key Vault referenced from App Service Configuration for higher security)
- [ ] `.gitignore` excludes any local `secrets.json` and `local.settings.json` files

## 9. Pre-Launch Smoke Checks

- [ ] `/` loads without authentication
- [ ] `/check-in` loads and functions without authentication
- [ ] `/staff` redirects unauthenticated users to Entra ID sign-in
- [ ] Staff Dashboard shows correct Total, Checked-In, Pending, Walk-In, and **Paid** counts
- [ ] Paid/Unpaid toggle persists after page reload (confirms Azure Table write path works end-to-end)
- [ ] Test registration triggers a confirmation email via ACS

## Known Risk / Hardening Opportunity

The current `RequireStaffRole` authorization policy only requires an **authenticated** Entra ID user from the configured tenant — it does not check for a specific App Role or group membership. For a small trusted-tenant deployment this may be acceptable, but before scaling staff access, consider adding an App Role (e.g., `Staff`) in the Entra ID app registration and changing the policy to `policy.RequireRole("Staff")`.
