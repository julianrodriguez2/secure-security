# Product Requirements Document

## SecureNotes Security Lab

**Version:** 1.0
**Status:** Ready for implementation
**Primary purpose:** Cybersecurity and cloud-security portfolio project
**Deployment model:** Local, containerized, cloud-ready
**Budget:** $0
**Primary platform:** Windows 11 with Docker Desktop
**Repository:** `secure-notes-security-lab`

---

## 1. Product summary

SecureNotes Security Lab is a locally hosted notes application designed to demonstrate practical application-security, identity, access-control, logging, monitoring, threat-modeling, vulnerability-management, and incident-response skills.

Users can authenticate and manage private notes. Every user must be isolated from every other user. Administrators can review security events and operational metrics but cannot read users’ private note content through administrative interfaces.

The project must demonstrate the complete security lifecycle:

1. Design a system and identify its assets.
2. Model realistic threats.
3. Implement preventive controls.
4. Generate security telemetry.
5. Detect suspicious behavior.
6. Test controls from an attacker’s perspective.
7. Conduct and document an incident-response exercise.
8. Provide evidence that each major control works.

The application itself should remain deliberately small. Security implementation, validation, and documentation are the primary deliverables.

---

## 2. Goals

The project must demonstrate:

- Secure authentication with an external identity provider.
- Role-based and object-level authorization.
- Strong isolation between users.
- Secure session handling.
- CSRF protection.
- Input validation and safe error handling.
- Least-privilege database access.
- Append-only audit events.
- Rate limiting and security headers.
- Structured application and security logs.
- Metrics, dashboards, traces, and alerting.
- Dependency, secret, container, static, and dynamic security scanning.
- A documented STRIDE threat model.
- A security-controls matrix.
- A risk register.
- A documented incident-response simulation.
- Reproducible setup through Docker Compose.
- Automated tests and continuous integration.

---

## 3. Non-goals

The initial release will not include:

- Public internet deployment.
- Paid cloud infrastructure.
- Payment processing.
- Social features.
- Note sharing.
- File uploads.
- Mobile applications.
- End-to-end encryption.
- Real email or SMS delivery.
- Kubernetes.
- Production-grade high availability.
- Support for multiple organizations or tenants.
- A custom identity provider.
- Offensive testing against systems not owned by the developer.

Any future cloud deployment must be treated as an optional extension and must not be required for the project to function.

---

## 4. Intended audience

The project is intended to be reviewed by:

- Entry-level cybersecurity hiring managers.
- Cloud-security hiring managers.
- Application-security teams.
- Security engineering teams.
- Software engineering hiring managers.
- Technical recruiters.
- Freelance clients seeking secure application development.

A reviewer should be able to clone the repository, follow the setup guide, run the application, run the automated tests, and reproduce the core security demonstrations.

---

## 5. Technology stack

### Application

- **Backend:** ASP.NET Core Web API
- **Language:** C#
- **Frontend:** React with TypeScript and Vite
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core
- **Reverse proxy:** Nginx
- **Identity provider:** Keycloak
- **Local orchestration:** Docker Compose

### Authentication architecture

Use a backend-for-frontend-style authentication architecture:

- Keycloak acts as the OpenID Connect identity provider.
- ASP.NET Core performs the authorization-code flow with PKCE.
- The application uses secure, HTTP-only authentication cookies.
- The browser must not persist access or refresh tokens in `localStorage`.
- The React frontend communicates with the API through the same Nginx origin.
- State-changing requests require antiforgery protection.

### Observability

- OpenTelemetry SDK
- OpenTelemetry Collector
- Prometheus
- Grafana
- Loki
- Structured JSON logs

### Security testing

- xUnit
- ASP.NET Core integration tests
- Playwright
- OWASP ZAP
- Semgrep
- Gitleaks
- Trivy
- Dependabot
- GitHub Actions

### Threat modeling and documentation

- OWASP Threat Dragon or a compatible diagramming tool
- Markdown documentation
- Mermaid diagrams where useful

---

## 6. High-level architecture

```text
Browser
   |
   | HTTPS
   v
Nginx
   |
   +--------------------+
   |                    |
   v                    v
React frontend     ASP.NET Core API/BFF
                        |
                        +---- Keycloak
                        |
                        +---- Application PostgreSQL
                        |
                        +---- OpenTelemetry Collector

Observability pipeline:

ASP.NET Core ----> OpenTelemetry Collector
Nginx ----------> Loki
Application ----> Loki
Collector ------> Prometheus
Prometheus -----> Grafana
Loki -----------> Grafana
```

All browser traffic must pass through Nginx. Internal databases and observability services must not expose ports to the host unless required for local development.

---

## 7. User roles

### Standard user

A standard user can:

- Sign in and sign out.
- View their own notes.
- Create notes.
- Edit their own notes.
- Delete their own notes.
- View basic information about their active session.
- Review a limited history of actions performed on their own notes.

A standard user cannot:

- View another user’s notes.
- Modify another user’s notes.
- supply or override ownership identifiers.
- Access administrative endpoints.
- Read application-wide security events.
- Change their own role.

### Security administrator

A security administrator can:

- View security-event metadata.
- View failed authentication and authorization statistics.
- View validation failures and server errors.
- View rate-limit events.
- View application health and security dashboards.
- Filter security events by event type, timestamp, route, and status.
- Disable a test user through the documented Keycloak process.

A security administrator cannot:

- View private note contents through administrative endpoints.
- Modify or delete audit records.
- Change application data directly through the UI.
- retrieve secrets, session cookies, passwords, or complete tokens.

---

## 8. Core functional requirements

### FR-1: Authentication

The system must:

- Redirect unauthenticated users to Keycloak.
- Use OpenID Connect authorization code flow with PKCE.
- Establish an HTTP-only application session after successful authentication.
- Provide logout that terminates the local application session.
- Handle expired sessions safely.
- Return `401 Unauthorized` for unauthenticated API requests.
- Return `403 Forbidden` for authenticated users lacking required roles.
- Avoid revealing whether a specific username exists.

### FR-2: User synchronization

On first successful login, the application must create a local user record containing:

- Internal application user ID.
- Keycloak subject identifier.
- Display name.
- Creation timestamp.
- Last successful login timestamp.

The application must not store the user’s Keycloak password.

### FR-3: Notes

Each note must contain:

- Note ID.
- Owner user ID.
- Title.
- Content.
- Created timestamp.
- Updated timestamp.
- Optional version or concurrency token.

Users must be able to:

- Create a note.
- List their notes.
- Retrieve one of their notes.
- Update one of their notes.
- Delete one of their notes.

The authenticated user’s identity must be derived from the trusted server-side session. Ownership must never be accepted from the request body or query string.

### FR-4: Object-level authorization

Every note query and mutation must include ownership verification.

A request for a note belonging to another user must:

- Return `404 Not Found` or `403 Forbidden` according to the documented security decision.
- Never return the title or content.
- Create an authorization-failure security event.
- Include a request correlation ID.
- Avoid logging note content or credentials.

Automated tests must prove that User A cannot read, modify, or delete User B’s notes.

### FR-5: Security events

The system must create structured security events for:

- Successful login.
- Logout.
- Failed authentication observed by the application.
- Authorization failure.
- Invalid antiforgery token.
- Input-validation failure.
- Rate-limit response.
- Unexpected application exception.
- Administrative security-dashboard access.
- Note creation, update, and deletion.
- User-disabled incident-response action when represented locally.

Each security event should contain:

- Event ID.
- Event type.
- Timestamp in UTC.
- Request correlation ID.
- Actor user ID when known.
- Route.
- HTTP method.
- Status code.
- Target resource identifier when appropriate.
- Safe client metadata where available.
- Sanitized event details.

Security events must never contain:

- Passwords.
- Authentication cookies.
- Complete access or refresh tokens.
- Authorization headers.
- Note content.
- Database connection strings.
- Secret values.
- Raw exception dumps returned to clients.

### FR-6: Audit-event immutability

Audit records must be append-only.

The application must not provide endpoints for updating or deleting audit events.

The database must enforce audit immutability through one or more of:

- Restricted database permissions.
- Database triggers rejecting updates and deletes.
- Separate database roles.
- Automated tests verifying mutation attempts fail.

### FR-7: Security dashboard

The administrator dashboard must display:

- Authentication-event count.
- Authorization-failure count.
- Validation-failure count.
- Rate-limit count.
- Server-error count.
- Most targeted API routes.
- Security events over time.
- Recent security events.
- Application health status.

The dashboard must not display private note contents.

### FR-8: Health endpoints

Provide:

- `/health/live`
- `/health/ready`

Liveness must confirm the process is running.

Readiness must verify critical dependencies such as PostgreSQL and Keycloak connectivity without exposing credentials or sensitive configuration.

---

## 9. Security requirements

### SR-1: Secure cookies

Authentication cookies must use:

- `HttpOnly`
- `Secure`
- An appropriate `SameSite` policy
- Limited lifetime
- Server-controlled invalidation where feasible

Production-mode configuration must refuse to send authentication cookies over plaintext HTTP.

### SR-2: CSRF protection

All state-changing browser requests must require a valid antiforgery token.

The project must include tests proving that a valid authenticated session without a valid antiforgery token cannot create, update, or delete a note.

### SR-3: Input validation

Validate all untrusted input.

Initial limits:

- Note title: 1–100 characters.
- Note content: 1–5,000 characters.
- Pagination size: maximum 100.
- Identifiers: valid expected format.
- JSON requests: reject malformed input.
- Content type: require the expected media type.
- Unknown privileged fields: reject or ignore according to documented behavior.

Validation errors must use a consistent Problem Details response and must not expose stack traces.

### SR-4: Output safety

React must render note content as text by default.

Do not use unsafe HTML rendering for note content.

Any future rich-text support is out of scope.

### SR-5: Database access

The application database account must:

- Have access only to the application database.
- Avoid PostgreSQL superuser privileges.
- Have only the permissions required by the application.
- Be separate from migration credentials where practical.
- Be separate from Keycloak’s database credentials.

### SR-6: Secrets

Secrets must:

- Be provided through environment variables, Docker secrets, or local secret stores.
- Never be committed to Git.
- Have placeholders in `.env.example`.
- Be detected by automated Gitleaks scans.
- Be redacted from logs and screenshots.

The repository must ignore:

- `.env`
- private keys
- generated certificates containing private keys
- database volumes
- local secret files
- test artifacts containing tokens
- Terraform state if Terraform is added later

### SR-7: Reverse-proxy controls

Nginx must implement:

- Local HTTPS.
- Request-body size limits.
- Rate limiting on API routes.
- Reasonable request timeouts.
- Removal or minimization of server-version disclosure.
- Security response headers.
- Proxy forwarding of correlation IDs.
- No direct public access to internal services.

Required headers include, where applicable:

- Content Security Policy.
- `X-Content-Type-Options`.
- `Referrer-Policy`.
- Frame restrictions through CSP or `X-Frame-Options`.
- Permissions Policy.
- Strict Transport Security in production-mode HTTPS environments.

### SR-8: Safe error handling

Client responses must not expose:

- Stack traces.
- SQL queries.
- Internal filesystem paths.
- Container names.
- Connection strings.
- Keycloak secrets.
- Internal exception details.

Unexpected failures must return a generic Problem Details response with a correlation ID.

### SR-9: Logging safety

Logging must use structured fields.

Sensitive values must be excluded or redacted.

The project must include automated or manual checks demonstrating that:

- Note contents are not logged.
- Cookies are not logged.
- Authorization headers are not logged.
- Passwords are not logged.
- Complete tokens are not logged.

### SR-10: Container security

Container images must:

- Use pinned or controlled base-image versions.
- Avoid running as root where supported.
- Contain only required runtime dependencies.
- Include health checks where useful.
- Use read-only filesystems where practical.
- Drop unnecessary Linux capabilities.
- Avoid privileged mode.
- Avoid mounting the Docker socket.
- Pass documented Trivy scans or have all accepted findings explained.

---

## 10. Observability requirements

The application must emit:

### Logs

- Structured JSON application logs.
- Security-event logs.
- Nginx access and error logs.
- Correlation IDs propagated across requests.

### Metrics

At minimum:

- Request count.
- Request duration.
- Response status count.
- Authentication events.
- Authorization failures.
- Validation failures.
- Rate-limit responses.
- Unhandled exceptions.
- Database dependency health.
- Active or recent sessions where safely measurable.

Metrics must not use unbounded labels such as raw user IDs, note IDs, tokens, or arbitrary request values.

### Traces

Distributed traces should cover:

- Incoming HTTP request.
- Authentication/session middleware.
- API handler.
- Database operation.
- Outbound Keycloak communication where supported.

Sensitive note content must not appear in trace attributes.

### Dashboards

Grafana must provide:

- Application overview.
- HTTP status and latency dashboard.
- Security-event dashboard.
- Dependency-health dashboard.

### Alerts

At minimum, create alert rules for:

- Five or more authorization failures within five minutes.
- Elevated `5xx` response rate.
- Repeated rate-limit events.
- Application readiness failure.
- PostgreSQL unavailable.

Local alerts may remain within Grafana. No paid notification service is required.

---

## 11. Testing requirements

### Unit tests

Cover:

- Input validation.
- Authorization-policy logic.
- Security-event sanitization.
- Safe error-response mapping.
- Note ownership rules.

### Integration tests

Cover:

- Database persistence.
- User synchronization.
- Note CRUD.
- Audit-event creation.
- Audit immutability.
- Authentication and role requirements.
- Antiforgery enforcement.

### End-to-end tests

Using two standard users and one administrator:

1. User A signs in.
2. User A creates a note.
3. User B signs in.
4. User B cannot read User A’s note.
5. User B cannot modify User A’s note.
6. User B cannot delete User A’s note.
7. The failed attempts appear as security events.
8. A standard user cannot open the security dashboard.
9. The administrator can open the security dashboard.
10. The administrator cannot retrieve note contents through admin endpoints.
11. State-changing requests without antiforgery protection fail.
12. Excess requests eventually receive `429 Too Many Requests`.

### Security scanning

CI and documented local commands must run:

- Gitleaks for secrets.
- Semgrep for static analysis.
- Trivy for container and dependency findings.
- .NET dependency vulnerability checks.
- npm dependency vulnerability checks.
- OWASP ZAP baseline scan.
- Dependabot configuration.

A vulnerability scan does not need to show zero findings. Every unresolved medium-or-higher finding must be documented as:

- Fixed.
- False positive.
- Accepted risk.
- Deferred with justification.

### Regression tests

Every confirmed vulnerability or broken security control must receive a regression test before being considered fixed.

---

## 12. Threat-model requirements

Create `security/threat-model.md` containing:

- System overview.
- Architecture diagram.
- Assets.
- Actors.
- Entry points.
- Trust boundaries.
- Data flows.
- STRIDE analysis.
- Risk scoring.
- Mitigations.
- Residual risks.
- Assumptions.
- Out-of-scope threats.

At minimum, analyze:

- Account compromise.
- Session theft.
- CSRF.
- Broken object-level authorization.
- Role escalation.
- Injection.
- Stored cross-site scripting.
- Brute force.
- API flooding.
- Sensitive-data leakage through logs.
- Secret leakage through Git.
- Vulnerable dependencies.
- Malicious container image.
- Database compromise.
- Audit-log tampering.
- Administrator misuse.
- Keycloak misconfiguration.
- Observability-system exposure.
- Denial of service against PostgreSQL or Keycloak.

Each threat must have:

- Threat ID.
- STRIDE category.
- Description.
- Affected asset.
- Likelihood score from 1–5.
- Impact score from 1–5.
- Total risk score.
- Existing controls.
- Planned controls.
- Residual risk.
- Validation method.

---

## 13. Incident-response requirements

Simulate this incident:

> A valid user session appears to have been compromised. The session is repeatedly attempting to access notes owned by other users.

Create:

- `security/incident-response-plan.md`
- `security/playbooks/compromised-session.md`
- `security/incidents/simulated-incident-report.md`
- `security/incidents/lessons-learned.md`

The simulation must include:

1. Preparation.
2. Alert generation.
3. Detection.
4. Initial triage.
5. Scope analysis.
6. Evidence collection.
7. Containment.
8. Session or user invalidation.
9. Verification that no unauthorized note content was returned.
10. Recovery.
11. Monitoring.
12. Lessons learned.
13. Improvements and regression tests.

Record:

- Attack start time.
- Detection time.
- Investigation start time.
- Containment time.
- Recovery time.
- Affected account.
- Targeted routes.
- Number of denied attempts.
- Whether any request succeeded.
- Relevant correlation IDs.
- Controls that worked.
- Controls that failed or required improvement.

---

## 14. Repository structure

```text
secure-notes-security-lab/
├── AGENTS.md
├── README.md
├── .env.example
├── .gitignore
├── docker-compose.yml
├── Directory.Build.props
├── SecureNotes.sln
├── docs/
│   ├── PRD.md
│   ├── IMPLEMENTATION_PLAN.md
│   ├── architecture.md
│   ├── setup.md
│   ├── demo-script.md
│   └── known-limitations.md
├── src/
│   ├── SecureNotes.Api/
│   └── SecureNotes.Web/
├── tests/
│   ├── SecureNotes.UnitTests/
│   ├── SecureNotes.IntegrationTests/
│   └── SecureNotes.E2E/
├── infrastructure/
│   ├── nginx/
│   ├── keycloak/
│   ├── postgres/
│   ├── otel/
│   ├── prometheus/
│   ├── loki/
│   └── grafana/
├── security/
│   ├── threat-model.md
│   ├── risk-register.md
│   ├── controls-matrix.md
│   ├── security-test-report.md
│   ├── incident-response-plan.md
│   ├── playbooks/
│   └── incidents/
├── scripts/
│   ├── dev-up.ps1
│   ├── dev-down.ps1
│   ├── test.ps1
│   ├── security-scan.ps1
│   └── generate-demo-events.ps1
├── evidence/
│   └── README.md
└── .github/
    ├── dependabot.yml
    └── workflows/
        ├── ci.yml
        └── security.yml
```

Generated evidence, credentials, tokens, private certificates, database volumes, and large scan outputs must not be committed unless they have been deliberately sanitized.

---

## 15. Implementation phases

### Phase 0 — Repository foundation and execution plan

**Deliverables**

- Repository structure.
- `AGENTS.md`.
- `docs/PRD.md`.
- `docs/IMPLEMENTATION_PLAN.md`.
- Solution and project scaffolding.
- Formatting and editor configuration.
- Base `.gitignore`.
- `.env.example`.
- Initial CI workflow skeleton.
- Initial README.

**Definition of done**

- The repository builds.
- Placeholder tests run successfully.
- No real secrets are committed.
- Codex has documented commands for build, test, lint, and startup.
- No application features are implemented yet.

---

### Phase 1 — Local infrastructure and application shell

**Deliverables**

- Docker Compose configuration.
- React application shell.
- ASP.NET Core API shell.
- Application PostgreSQL.
- Keycloak and its PostgreSQL database.
- Nginx reverse proxy.
- Local HTTPS development setup.
- Liveness and readiness endpoints.
- PowerShell startup and shutdown scripts.

**Definition of done**

- `docker compose up --build` starts the environment.
- The frontend loads through Nginx.
- The API health endpoints work through Nginx.
- PostgreSQL and Keycloak are not unnecessarily exposed.
- The readiness endpoint reports dependency failures safely.
- Setup instructions work from a clean clone.

---

### Phase 2 — Authentication, sessions, and roles

**Deliverables**

- Keycloak realm configuration.
- Standard-user and security-administrator roles.
- OIDC authorization-code flow with PKCE.
- Secure HTTP-only session cookie.
- Login and logout flows.
- Local user synchronization.
- Antiforgery foundation.
- Authentication tests.

**Definition of done**

- A user can sign in and sign out.
- An unauthenticated API request returns `401`.
- A standard user cannot access an administrator endpoint.
- An administrator can access a protected test endpoint.
- Browser tokens are not stored in `localStorage`.
- Sensitive authentication data does not appear in logs.

---

### Phase 3 — Notes and authorization isolation

**Deliverables**

- Notes database schema.
- Entity Framework migrations.
- Note CRUD APIs.
- React notes interface.
- Object-level authorization.
- Validation and Problem Details.
- Unit and integration tests.
- Two-user isolation end-to-end test.

**Definition of done**

- Users can manage their own notes.
- User A cannot read, update, or delete User B’s note.
- Ownership comes from the authenticated session.
- Authorization failures are safe and test-covered.
- Invalid or oversized input is rejected.
- Note content is not written to logs.

---

### Phase 4 — Audit and security-event foundation

**Deliverables**

- Append-only audit-event schema.
- Security-event service.
- Correlation-ID middleware.
- Login, logout, authorization, validation, note-change, and exception events.
- Database-level immutability control.
- User activity view.
- Administrator security-event API.

**Definition of done**

- Required events are created.
- Audit records cannot be updated or deleted by the application role.
- Security events contain no prohibited sensitive data.
- Standard users cannot query application-wide events.
- Administrator responses contain no note content.
- Immutability and sanitization tests pass.

---

### Phase 5 — Edge and application hardening

**Deliverables**

- Nginx rate limiting.
- Request-size limits.
- Security headers.
- Safe proxy configuration.
- Complete antiforgery enforcement.
- Secure cookie configuration.
- Safe exception handling.
- Restricted database roles.
- Container hardening.
- Security configuration documentation.

**Definition of done**

- Excess requests receive `429`.
- State-changing requests without antiforgery tokens fail.
- Security headers are present.
- Client errors contain no internal implementation details.
- Containers avoid privileged operation.
- Application and Keycloak use separate database credentials.
- Relevant controls have automated tests.

---

### Phase 6 — Observability and security dashboard

**Deliverables**

- Structured JSON logging.
- OpenTelemetry instrumentation.
- OpenTelemetry Collector.
- Prometheus metrics.
- Loki log aggregation.
- Grafana dashboards.
- Alert rules.
- Administrator security dashboard.

**Definition of done**

- Logs, metrics, and traces are visible.
- Correlation IDs connect application events.
- The required security metrics exist.
- Sensitive values do not appear in telemetry.
- The authorization-failure scenario triggers an alert.
- Dashboards contain no note content or secrets.

---

### Phase 7 — Automated security testing

**Deliverables**

- Gitleaks configuration.
- Semgrep configuration.
- Trivy scans.
- Dependency scans.
- OWASP ZAP baseline scan.
- GitHub Actions security workflow.
- Dependabot configuration.
- Security-test report.
- Remediation commits and regression tests.

**Definition of done**

- Documented local scan commands work.
- CI runs applicable scans.
- Findings are classified and addressed.
- No known committed secrets remain.
- No unresolved critical findings remain.
- Medium and high findings have documented dispositions.

---

### Phase 8 — Threat model, incident exercise, and portfolio release

**Deliverables**

- Final architecture documentation.
- STRIDE threat model.
- Risk register.
- Security-controls matrix.
- Incident-response plan.
- Compromised-session playbook.
- Conducted incident simulation.
- Incident report.
- Lessons learned.
- Demo script.
- Screenshots or sanitized evidence.
- Final README.
- Known-limitations document.

**Definition of done**

- Every major threat maps to a control and validation method.
- The simulated incident is detectable and reproducible.
- The response timeline is documented.
- Regression tests cover discovered weaknesses.
- Setup succeeds from a clean clone.
- CI is green.
- The repository contains no secrets or sensitive evidence.
- A five-to-eight-minute portfolio demonstration can be completed using the demo script.

---

## 16. Security-controls matrix requirement

Create `security/controls-matrix.md` with these columns:

| Control ID | Threat | Control | Implementation | Validation | Evidence | Status |
| ---------- | ------ | ------- | -------------- | ---------- | -------- | ------ |

Every major control must be linked to:

- The threat it addresses.
- The implementing file or component.
- The test or manual procedure that validates it.
- Sanitized evidence.
- Current implementation status.

The project must demonstrate:

```text
Threat → Security control → Code/configuration → Test → Evidence
```

---

## 17. CI requirements

Pull-request and main-branch workflows must run applicable checks:

- Restore dependencies.
- Build backend.
- Run backend unit tests.
- Run backend integration tests where feasible.
- Install frontend dependencies.
- Run frontend linting and type checking.
- Build frontend.
- Run secret scanning.
- Run static analysis.
- Run dependency vulnerability checks.
- Build and scan container images where feasible.

CI must fail when:

- The application does not build.
- Required tests fail.
- A verified secret is detected.
- A critical unapproved security finding is detected.
- Formatting or type checks fail.

---

## 18. Documentation requirements

The final repository must document:

- Prerequisites.
- Clean installation.
- Environment configuration.
- Starting and stopping services.
- Test-user creation.
- Role assignment.
- Running tests.
- Running security scans.
- Viewing Grafana.
- Reproducing authorization failures.
- Triggering rate limits.
- Conducting the incident simulation.
- Resetting the environment.
- Destroying local data.
- Known security limitations.
- Cost statement confirming that no paid services are required.

Documentation must not contain real secrets or active session values.

---

## 19. Portfolio demonstration

The final demonstration should show:

1. Architecture and trust boundaries.
2. Login through Keycloak.
3. User A creating a note.
4. User B attempting to access User A’s note.
5. The access attempt being denied.
6. The related security event in logs.
7. The authorization-failure metric.
8. The triggered Grafana alert.
9. The administrator dashboard.
10. The append-only audit control.
11. Security scan results.
12. The threat model.
13. The incident-response timeline.
14. The controls matrix linking threats to evidence.

---

## 20. Global definition of done

The project is complete when:

- The system runs locally without paid services.
- All core functionality works.
- Authentication and authorization tests pass.
- Two-user isolation is proven.
- Audit records are append-only.
- Logs exclude sensitive content.
- Security telemetry is visible.
- Required alerts trigger.
- Security scans are automated and documented.
- Threat-model and incident-response artifacts are complete.
- The complete environment can be reproduced from repository instructions.
- The application can be demonstrated without modifying source code.
- The repository is safe to make public.
