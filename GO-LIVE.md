# Go-Live Checklist — The Acting Collective

Run through this checklist in order on launch day. Each section builds on the previous one — don't skip ahead.

## T-minus 1 day: Final verification

- [ ] `PRODUCTION-CHECKLIST.md` fully completed
- [ ] `dotnet build` / CI pipeline green with 0 errors, 0 warnings you haven't consciously accepted
- [ ] GitHub Actions workflow (`deploy.yml`) has run successfully at least once against a **staging** slot
- [ ] Azure App Service staging slot smoke-tested (registration, check-in, staff sign-in, email delivery)
- [ ] Cloudflare DNS records confirmed propagated (`dig`/`nslookup` the production hostname)
- [ ] TLS certificate valid on the production hostname (Cloudflare Full Strict + App Service Managed Certificate)

## Launch Day: Deployment

1. [ ] Merge final release branch into `main` (or trigger the release workflow)
2. [ ] Confirm GitHub Actions deployment completes successfully
3. [ ] If using a staging slot, **swap** staging → production
4. [ ] Confirm the production URL resolves and serves the app (check response headers for `Server` / app version if exposed)

## Launch Day: Smoke Test (Production)

- [ ] `/` — public landing page loads, no console errors, registration form submits successfully
- [ ] `/check-in` — check-in flow works for a real test registration, no authentication prompt appears
- [ ] `/staff` — unauthenticated visit redirects to Microsoft Entra ID sign-in
- [ ] Sign in with a real staff Entra ID account — dashboard loads
- [ ] Staff Dashboard stat cards show correct counts: Total Registered, Checked In, Not Yet Arrived, Walk-Ins, **Paid**
- [ ] Toggle a test registration's Paid/Unpaid status — reload the page and confirm it persisted (validates Azure Table Storage write path)
- [ ] Check in a test registration — reload and confirm it persisted
- [ ] Register a walk-in from the Staff Dashboard — confirm it appears in the roster
- [ ] Submit a real registration through `/` and confirm the confirmation email arrives (check spam folder too — validates SPF/DKIM/DMARC)

## Launch Day: Monitoring & Rollback Readiness

- [ ] Application Insights (or App Service logs) actively streaming — confirm you can see live requests
- [ ] Alert rule configured for HTTP 5xx spikes or App Service downtime
- [ ] Previous known-good deployment slot/artifact identified and ready for rollback swap if issues arise
- [ ] Team communication channel open (Slack/Teams) for the launch window with clear owner for "go/no-go" decisions

## Post-Launch (First 24 Hours)

- [ ] Monitor registration volume vs. expected traffic
- [ ] Spot-check a handful of real registrations in the Staff Dashboard for data correctness
- [ ] Confirm no unexpected authentication failures on `/staff` (check App Service logs for 401/403 patterns)
- [ ] Confirm ACS email send success rate (Azure Portal → Communication Services → Metrics)
- [ ] Review Cloudflare analytics for anomalous traffic or blocked requests

## Post-Launch (First Week)

- [ ] Rotate any credentials that were shared during setup/testing (Storage keys, ACS connection string) if there's any doubt about exposure
- [ ] Revisit the "Known Risk" item in `PRODUCTION-CHECKLIST.md` regarding staff authorization scope (App Role vs. any-authenticated-user)
- [ ] Archive/clean up any temporary staging resources no longer needed
- [ ] Retrospective: capture anything that should change before the next event/class cycle
