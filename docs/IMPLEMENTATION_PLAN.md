# SecureNotes implementation plan — Phases 0–8

## Purpose and current state

This document is the authoritative execution plan for SecureNotes Security Lab. Update it whenever implementation differs from the PRD or an unresolved risk is discovered.

### Repository inspection — 2026-08-03

- Present: AGENTS.md and docs/prd.md only.
- Absent: Git metadata, solution/projects, tests, Docker Compose, source code, security tooling configuration, CI, and prior implementation plan.
- No application phase has been implemented.
- This plan is documentation only; it adds no runtime dependency or application behavior.

### Scope and constraints

- The project remains fully local, containerized, reproducible, and free. Do not deploy publicly or introduce paid services.
- Implement phases in order unless this plan documents a concrete, security-reviewed reason to deviate.
- Treat all browser, URL, header, form, JSON, token-claim, and database values as untrusted.
- Derive identity and ownership only from validated server-side session context.
- Never commit credentials, private certificates, database volumes, tokens, or unsanitized evidence.

## Locked architecture and interface decisions

### Platform and containers

- Use .NET 10 LTS, Node.js 24 LTS, PostgreSQL 18, Keycloak 26.7.0, React/TypeScript/Vite, ASP.NET Core, EF Core, Nginx, Docker Compose, OpenTelemetry Collector, Prometheus, Loki, and Grafana.
- Pin SDK/package versions in lock files and container images by explicit version plus digest. Record upgrades and scan dispositions in security/risk-register.md.
- Runtime services: nginx, api, postgres-app, keycloak, postgres-keycloak, otel-collector, prometheus, loki, and grafana.
- api-migrator is a short-lived migration-only service. zap-baseline exists only in a Compose security profile.
- Nginx is the sole application ingress at HTTPS localhost. It serves the React build at /, proxies /api/ and /health/ to API, and /auth/ to Keycloak.
- Only Nginx HTTPS and localhost-only Grafana may publish host ports. PostgreSQL, Keycloak, Prometheus, Loki, and the collector are internal.
- Use isolated internal Compose networks for application, application database, identity, Keycloak database, telemetry, and observability. Nginx reaches only API and Keycloak. API reaches only its database, Keycloak, and the collector.
- Use mkcert for a trusted local certificate. Ignore generated certificate/key material. Production-mode startup must fail if it could issue authentication cookies over HTTP.
- Use non-root images where supported, read-only filesystems with explicit writable mounts/tmpfs where practical, dropped capabilities, health checks, no privileged mode, and no Docker socket.

### Authentication, authorization, and API contract

- Keycloak is the OIDC provider. ASP.NET Core is the BFF and uses authorization code flow with PKCE. Keycloak redirects stay same-origin through /auth/.
- Issue only an opaque __Host- session cookie: Secure, HttpOnly, SameSite=Strict, Path=/, limited lifetime, no Domain attribute. Store its random identifier only as a hash with server-side session metadata, so logout, expiration, user disablement, and incident response can revoke it.
- Keep access and refresh tokens server-side. Never place them or equivalent data in browser storage, logs, API responses, screenshots, or evidence.
- Provide GET /api/antiforgery/token to authenticated callers. React sends the returned value in X-CSRF-TOKEN. Require it on every browser state change, including logout.
- API endpoints: POST /auth/login, POST /auth/logout, GET /api/session, GET /api/antiforgery/token, GET|POST /api/notes, GET|PUT|DELETE /api/notes/{noteId}, GET /api/me/activity, GET /api/admin/security-summary, GET /api/admin/security-events, GET /health/live, and GET /health/ready.
- Note write requests contain only title and content. Do not accept owner IDs. Use UUID IDs, UTC timestamps, title 1–100 characters, content 1–5,000 characters, and pageSize 1–100. Initial release exposes no concurrency-token API.
- Return 404 for a cross-user note ID and record the authorization failure. Anonymous calls receive 401; authenticated users without the required role receive 403.
- Return sanitized application/problem+json errors with correlation IDs. Never return stack traces, SQL, paths, container names, connection strings, secrets, or raw exception details.

### Data, audit, and observability contract

- ApplicationUsers: internal UUID, unique Keycloak subject, display name, CreatedAtUtc, LastSuccessfulLoginAtUtc. Never store passwords.
- Notes: UUID, owner-user foreign key, title, content, CreatedAtUtc, UpdatedAtUtc.
- Use a single append-only SecurityEvents ledger for security and activity audit records. PostgreSQL triggers reject UPDATE and DELETE; the runtime role lacks mutation grants; no mutation endpoint exists.
- Events use only allowlisted metadata: event ID/type, UTC timestamp, correlation ID, known actor ID, route template, method, status, target ID where appropriate, and bounded safe client details.
- Events, logs, metrics, traces, dashboards, and client errors must never expose note content, passwords, cookies, authorization headers, complete tokens, connection strings, secret values, or raw exception dumps.
- Metrics use bounded labels only; never use raw user IDs, note IDs, token values, arbitrary input, or untemplated routes as labels.

## Phase 0 — Repository foundation and execution plan

**Depends on:** none.

### Milestones

1. Normalize docs/prd.md to docs/PRD.md before Linux CI. Preserve its content.
2. Initialize local Git metadata if absent; do not create a remote repository.
3. Add .gitignore, .gitattributes, .editorconfig, .env.example, global.json, Directory.Build.props, SecureNotes.sln, README, and CI/Dependabot skeletons.
4. Scaffold nullable-enabled empty projects: src/SecureNotes.Api, src/SecureNotes.Web, tests/SecureNotes.UnitTests, tests/SecureNotes.IntegrationTests, and tests/SecureNotes.E2E. Add placeholder tests only.
5. Create the documented directory layout: docs, infrastructure, security, scripts, evidence, and .github.
6. Ignore local environment files, private keys/certificates, volume data, token-containing test artifacts, generated evidence, scan output, and future Terraform state.

### Expected files and projects

SecureNotes.sln; Directory.Build.props; global.json; .editorconfig; .gitattributes; .gitignore; .env.example; README.md; .github/workflows/ci.yml; .github/workflows/security.yml; .github/dependabot.yml; project manifests; docs/architecture.md; docs/setup.md; docs/demo-script.md; docs/known-limitations.md; security and infrastructure placeholders; evidence/README.md.

### Tests and validation

    dotnet format --verify-no-changes
    dotnet build SecureNotes.sln -warnaserror
    dotnet test SecureNotes.sln
    npm ci --prefix src/SecureNotes.Web
    npm run lint --prefix src/SecureNotes.Web
    npm run typecheck --prefix src/SecureNotes.Web
    npm run build --prefix src/SecureNotes.Web
    gitleaks detect --source .

Definition of done: solution builds, placeholder tests pass, no real secrets are present, and local build/test/lint/startup commands are documented.

## Phase 1 — Local infrastructure and application shell

**Depends on:** Phase 0.

### Milestones

1. Add Docker Compose, Dockerfiles, Nginx, application/Keycloak PostgreSQL bootstrap configuration, baseline OpenTelemetry/Prometheus/Loki/Grafana configuration, and dev-up/dev-down PowerShell scripts.
2. Build React to static assets served only by Nginx; do not include a development server in the runtime stack.
3. Add /health/live and /health/ready. Liveness confirms the process; readiness safely checks app PostgreSQL and Keycloak connectivity without configuration disclosure.
4. Configure HTTPS, Nginx routing, correlation-ID forwarding, service health checks, named volumes, and startup ordering.
5. Document clean-clone prerequisites, mkcert setup, startup/shutdown, reset, and local-data destruction.

### Expected files and containers

docker-compose.yml; Dockerfiles for API and web build; infrastructure/nginx; infrastructure/keycloak; infrastructure/postgres; infrastructure/otel; infrastructure/prometheus; infrastructure/loki; infrastructure/grafana; scripts/dev-up.ps1; scripts/dev-down.ps1; docs/setup.md.

### Tests and validation

    docker compose config
    docker compose build
    docker compose up --build --wait
    curl.exe https://localhost/health/live
    curl.exe https://localhost/health/ready
    docker compose ps
    docker compose down

Test health success, safe readiness failure, Nginx-only application ingress, and frontend/API availability through Nginx.

Definition of done: Compose starts; frontend and health endpoints work through Nginx; databases/Keycloak are not unnecessarily exposed; setup works from a clean clone.

## Phase 2 — Authentication, sessions, and roles

**Depends on:** Phase 1.

### Milestones

1. Add versioned Keycloak realm import with standard-user and security-administrator roles, strict redirect URIs, PKCE enforcement, and placeholder-only bootstrap administration.
2. Implement OIDC callback/login/logout, local user synchronization, opaque session persistence/revocation, expiry handling, and explicit authorization policies.
3. Add redacted authentication/session event handling. Capture BFF-observed protocol failures as application events; retain Keycloak credential failures in redacted identity telemetry.
4. Add basic React session UI and an administrator-only protected test route. Do not persist tokens in browser storage.
5. Use realm import plus test-only Keycloak Admin API setup/teardown to provision two standard users and one administrator for integration/E2E tests.

### Expected files/components

Keycloak realm JSON; API authentication, authorization, user-sync, session services and EF migrations; role-policy configuration; React session UI; authentication unit, integration, and Playwright tests.

### Tests and validation

    dotnet test tests/SecureNotes.UnitTests --filter Category=Authentication
    dotnet test tests/SecureNotes.IntegrationTests --filter Category=Authentication
    npm run test:e2e --prefix tests/SecureNotes.E2E -- --grep "authentication|roles"
    docker compose up --build --wait

Prove login creates/synchronizes a user without a password; logout/expiry revoke the session; anonymous calls are 401; standard users receive 403 on admin routes; administrators succeed; cookies have required flags; and logs exclude passwords, cookies, headers, and complete tokens.

Definition of done: sign-in/sign-out, roles, session safety, token-storage absence, and authentication log safety are demonstrated.

## Phase 3 — Notes, validation, and object-level isolation

**Depends on:** Phase 2.

### Milestones

1. Add Notes EF schema/migration. api-migrator alone applies migrations; runtime API uses the restricted application account.
2. Implement thin CRUD endpoints using a testable ownership service. Every note read, update, and delete query includes the authenticated local user ID.
3. Implement React list/create/edit/delete UI with text-only note rendering.
4. Add input validation and Problem Details for malformed JSON, wrong content type, invalid IDs, invalid pagination, unknown privileged fields, oversized values, and unexpected errors.
5. Add GET /api/me/activity projection limited to safe event metadata for the current authenticated user.

### Expected files/components

Note entities/configuration/migrations; note service and endpoints; validation and Problem Details middleware; notes UI; unit/integration/E2E tests.

### Tests and validation

    dotnet test tests/SecureNotes.UnitTests --filter Category=Notes
    dotnet test tests/SecureNotes.IntegrationTests --filter Category=Notes
    npm run test:e2e --prefix tests/SecureNotes.E2E -- --grep "notes|isolation|validation"
    dotnet build SecureNotes.sln -warnaserror

Prove CRUD, two-user read/update/delete denial, no cross-user title/content disclosure, client ownership rejection/ignore behavior, validation limits, malformed requests, and no note content in captured logs.

Definition of done: users can manage only their own notes and all isolation cases have automated regression coverage.

## Phase 4 — Audit and security-event foundation

**Depends on:** Phases 2 and 3.

### Milestones

1. Add append-only SecurityEvents schema/migration, PostgreSQL UPDATE/DELETE rejection triggers, and runtime database grants that prohibit audit mutation.
2. Add correlation-ID middleware that validates trusted proxy input or creates a bounded ID, returns it to clients, and propagates it into events/logs/traces.
3. Emit events for login/logout, BFF authentication failure, authorization failure, invalid antiforgery token, validation failure, rate limit, unhandled exception, admin access, note changes, and represented user-disable action.
4. Add an event sanitizer/allowlist and bounded safe client metadata.
5. Add user-activity and administrator security event/summary queries. Admin filters allow only event type, UTC time range, route template, and status; projections never contain note titles/content.

### Expected files/components

Security event domain/service/migrations/trigger scripts; correlation middleware; sanitizer; admin query endpoints/UI contracts; user activity query; security-event and immutability tests.

### Tests and validation

    dotnet test tests/SecureNotes.UnitTests --filter Category=SecurityEvents
    dotnet test tests/SecureNotes.IntegrationTests --filter Category="Audit|Immutability|Authorization"
    dotnet ef migrations script --project src/SecureNotes.Api

Prove required event creation with UTC/correlation data, failed audit mutation attempts, prohibited-field sanitization, administrator metadata-only responses, and standard-user denial of global event access.

Definition of done: required events are safe, append-only, and authorization-protected.

## Phase 5 — Edge and application hardening

**Depends on:** Phases 1–4.

### Milestones

1. Complete Nginx rate limits with 429 responses, request-body limits, timeouts, security headers, server-version minimization, trusted proxy configuration, and no internal service routes.
2. Enforce antiforgery on every browser mutation, validate cookie production settings, and reject production HTTP startup.
3. Complete generic exception handling and correlation-aware Problem Details.
4. Finalize separate application, migration, and Keycloak database credentials/permissions.
5. Harden Dockerfiles/Compose and document all security configuration and intentional port exposure.

### Tests and validation

    docker compose config
    docker compose up --build --wait
    dotnet test tests/SecureNotes.IntegrationTests --filter Category="Csrf|Hardening|RateLimit|Errors"
    npm run test:e2e --prefix tests/SecureNotes.E2E -- --grep "csrf|rate limit|headers"
    trivy config .

Prove missing/invalid CSRF tokens fail create/update/delete/logout; excess requests generate 429/events; required headers exist; production HTTP config is rejected; generic errors disclose no internals; and runtime DB credentials cannot migrate or mutate audit events.

Definition of done: edge, session, error, database, and container controls are test-covered and documented.

## Phase 6 — Observability and security dashboard

**Depends on:** Phases 4 and 5.

### Milestones

1. Add structured JSON logging, OpenTelemetry instrumentation, collector pipelines, Prometheus scraping, Loki ingestion, Grafana data sources, and dashboard provisioning.
2. Emit request count/duration/status, authentication, authorization, validation, rate-limit, exception, dependency-health, and safe bounded session metrics.
3. Provide dashboards for application overview, HTTP status/latency, security events, and dependency health.
4. Add local Grafana alert rules for five authorization failures in five minutes, elevated 5xx, repeated rate-limit events, readiness failure, and PostgreSQL unavailability.
5. Implement administrator dashboard from safe aggregate/projection APIs only.

### Tests and validation

    docker compose up --build --wait
    dotnet test tests/SecureNotes.IntegrationTests --filter Category=Observability
    npm run test:e2e --prefix tests/SecureNotes.E2E -- --grep "security dashboard"
    docker compose exec prometheus wget -qO- http://localhost:9090/-/ready

Trigger a cross-user attempt and verify its correlation across event, log, metric, trace, dashboard, and alert. Verify telemetry/dashboard data excludes note content and secrets; standard users cannot access dashboard data; administrators cannot retrieve notes through it.

Definition of done: required logs/metrics/traces/dashboards/alerts work without sensitive telemetry exposure.

## Phase 7 — Automated security testing and CI

**Depends on:** Phases 1, 5, and 6.

### Milestones

1. Add Gitleaks configuration, Semgrep configuration/rules, Trivy scans, dependency checks, local-only ZAP baseline profile, security-scan PowerShell script, CI security workflow, Dependabot, and security-test report.
2. CI runs restore/build/test, frontend install/lint/typecheck/build, secret/static/dependency/container/config scans, and feasible integration checks.
3. Restrict ZAP to the local Nginx Compose service. Do not scan third-party systems.
4. Classify each medium/high finding as fixed, false positive, accepted risk, or deferred with owner, justification, and review date. Fail CI for verified secrets, unapproved critical findings, and medium/high findings without a documented disposition.

### Expected files/components

.gitleaks.toml; Semgrep configuration; scripts/security-scan.ps1; ZAP profile; .github workflows; Dependabot; security/security-test-report.md; risk-register updates.

### Tests and validation

    gitleaks detect --source .
    semgrep scan --config .semgrep.yml
    trivy config .
    trivy fs --scanners vuln,secret,misconfig .
    trivy image secure-notes-api:local
    trivy image secure-notes-nginx:local
    dotnet list SecureNotes.sln package --vulnerable --include-transitive
    npm audit --prefix src/SecureNotes.Web --audit-level=high
    docker compose --profile security run --rm zap-baseline

Definition of done: documented scans work locally and in CI, no committed real secrets remain, no critical finding is unresolved, and all medium/high findings have a recorded disposition.

## Phase 8 — Threat model, incident exercise, and portfolio release

**Depends on:** Phases 0–7.

### Milestones

1. Complete architecture, setup, demo, limitations, README, and evidence documentation.
2. Create security/threat-model.md with PRD-required STRIDE threats, assets, actors, boundaries, data flows, risk scores, controls, residual risks, and validation methods.
3. Complete security/risk-register.md and security/controls-matrix.md. The controls matrix maps Threat → Control → implementation → validation → sanitized evidence → status.
4. Create security/incident-response-plan.md, security/playbooks/compromised-session.md, security/incidents/simulated-incident-report.md, and security/incidents/lessons-learned.md.
5. Perform the local compromised-session exercise: User A repeatedly requests User B notes; record attack/detection/investigation/containment/recovery times, correlation IDs, targeted routes, denied count, proof no request succeeded, session/user invalidation, monitoring, lessons, and regression improvements.
6. Produce a five-to-eight-minute demo and sanitized evidence index.

### Tests and validation

    dotnet format --verify-no-changes
    dotnet build SecureNotes.sln -warnaserror
    dotnet test SecureNotes.sln
    npm ci --prefix src/SecureNotes.Web
    npm run lint --prefix src/SecureNotes.Web
    npm run typecheck --prefix src/SecureNotes.Web
    npm run build --prefix src/SecureNotes.Web
    docker compose config
    docker compose up --build --wait
    gitleaks detect --source .
    semgrep scan --config .semgrep.yml
    trivy config .
    trivy image secure-notes-api:local
    docker compose --profile security run --rm zap-baseline

Definition of done: setup succeeds from a clean clone; CI is green; all major controls link to validation/evidence; the incident is reproducible; no secrets or unsanitized evidence remain; and the demo runs without source changes.

## High-risk controls

| Area | Required control | Primary evidence |
| --- | --- | --- |
| OIDC/session | PKCE, same-origin callback, opaque revocable server session, no browser tokens | Real Keycloak integration and Playwright storage/cookie checks |
| Cookies/CSRF | __Host- cookie, Secure, HttpOnly, Strict, bounded lifetime, CSRF on all mutations | Cookie inspection and negative request tests |
| Object authorization | Authenticated local user scopes every note query/mutation; cross-user 404 | Two-user integration/E2E attack tests |
| Audit integrity | Trigger plus restricted runtime role and no mutation routes | Database/application mutation attempts |
| Telemetry redaction | Allowlisted event/log/trace fields and bounded labels | Captured sink/export/dashboard tests |
| Containers/networking | Internal networks, least privilege, pinned images, no privileged/Docker socket | Compose inspection and Trivy config/image scans |

## Progress checklist

- [x] Inspect current repository state.
- [x] Create this implementation plan.
- [x] Phase 0 implemented and validated.
- [ ] Phase 1 implemented and validated.
- [ ] Phase 2 implemented and validated.
- [ ] Phase 3 implemented and validated.
- [ ] Phase 4 implemented and validated.
- [ ] Phase 5 implemented and validated.
- [ ] Phase 6 implemented and validated.
- [ ] Phase 7 implemented and validated.
- [ ] Phase 8 implemented and validated.

## Decision log and unresolved items

| Decision | Rationale | Status |
| --- | --- | --- |
| mkcert local HTTPS | Tests secure-cookie behavior locally without cost | Accepted |
| Proxy Keycloak under /auth/ | Keeps OIDC browser flow behind Nginx and same-origin | Accepted |
| Opaque server-revocable sessions | Supports logout and compromised-session containment without browser token storage | Accepted |
| 404 for cross-user notes | Avoids confirming note existence while preserving internal security evidence | Accepted |
| Trigger plus grants for audit immutability | Provides database-level defense in depth | Accepted |
| Grafana localhost-only port | Supports reviewers while retaining internal observability isolation | Accepted |
| Findings disposition policy | Enforces accountability without treating all third-party scan results as immediately fixable | Accepted |
| Phase 0 frontend toolchain | Use Vite 6.4.3, React 19.2.8, ESLint 9.39.5, and TypeScript 5.9.3; this keeps Vite compatible with Node 22 and resolves peer dependencies without overrides. Clean installations require Node 22.13 or newer. | Accepted |
| Phase 0 API scope | Keep the ASP.NET Core host endpoint-free; placeholder unit/integration tests prove project discovery only | Accepted |

- Phase 0 completed on 2026-08-03: local Git metadata, PRD filename normalization, .NET/API/test projects, React/Vite scaffold, lint/type-check/build scripts, CI skeleton, security-focused ignore rules, required placeholder directories, and setup documentation are present. No authentication, notes, infrastructure, telemetry, or other later-phase behavior was added.
- The PRD filename is now docs/PRD.md and local Git metadata is initialized. No remote configuration or commit was created.
- No other implementation decisions remain unresolved. Record all future deviations, security impact, affected validation, and rationale in this document.
