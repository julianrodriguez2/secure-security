# AGENTS.md

## Project mission

Build SecureNotes Security Lab according to `docs/PRD.md`.

This is a cybersecurity portfolio project. Security controls, tests, documentation, and reproducibility are more important than adding product features.

## Source of truth

Before making changes, read:

1. `docs/PRD.md`
2. `docs/IMPLEMENTATION_PLAN.md`, when present
3. Relevant existing source files and tests
4. The nearest nested `AGENTS.md`, when present

Do not silently contradict the PRD. Record necessary deviations in the implementation plan and explain them in the final task summary.

## Work boundaries

- Implement only the requested phase or task.
- Do not begin future phases without an explicit request.
- Do not replace the selected architecture without documenting a concrete blocker.
- Do not introduce paid cloud services.
- Do not deploy anything publicly.
- Do not use external systems the repository does not own.
- Do not perform offensive testing against third-party targets.
- Do not add unnecessary product features.
- Prefer simple, maintainable implementations over clever abstractions.

## Planning

For changes spanning multiple components, create or update an execution plan before implementation.

The plan must include:

- Goal.
- Current repository state.
- Files and components affected.
- Security implications.
- Implementation milestones.
- Tests.
- Validation commands.
- Decisions and tradeoffs.
- Progress and unresolved issues.

Keep `docs/IMPLEMENTATION_PLAN.md` current as work proceeds.

## Security rules

- Treat all browser, header, URL, form, JSON, token-claim, and database data as untrusted.
- Derive user identity from the validated server-side authentication context.
- Never trust an ownership ID supplied by a client.
- Enforce authorization on the server for every protected resource.
- Use deny-by-default authorization.
- Never log passwords, cookies, authorization headers, complete tokens, connection strings, secret values, or note contents.
- Never commit secrets.
- Never place real credentials in examples or tests.
- Use placeholder values in `.env.example`.
- Use parameterized database operations through Entity Framework Core.
- Return generic client errors with correlation IDs.
- Add a regression test for every repaired security defect.
- Prefer least-privilege container and database configuration.
- Do not weaken security controls merely to make tests pass.
- Do not suppress scan findings without a written justification.

## Authentication rules

- Use Keycloak as the OpenID Connect provider.
- Use authorization code flow with PKCE.
- Prefer a backend-for-frontend pattern with secure HTTP-only cookies.
- Do not persist access or refresh tokens in browser local storage.
- Require antiforgery protection for state-changing browser requests.
- Use explicit role policies for administrative routes.

## Data-isolation rules

Every note read, update, and delete operation must include the authenticated application user ID in its authorization decision.

Tests must prove:

- User A cannot read User B’s note.
- User A cannot update User B’s note.
- User A cannot delete User B’s note.
- Administrators cannot obtain note content through security endpoints.
- Client-supplied ownership identifiers cannot change ownership.

## Audit rules

Audit and security events are append-only.

- Do not create update or delete endpoints for audit events.
- Add database enforcement against mutation.
- Sanitize event properties.
- Use UTC timestamps.
- Include correlation IDs.
- Avoid sensitive or unbounded telemetry labels.

## Dependency rules

- Ask before adding a substantial new production dependency unless the PRD explicitly requires it.
- Prefer well-maintained official packages.
- Pin container images to controlled versions.
- Keep lock files committed.
- Document why every security tool or infrastructure service is included.

## Code quality

### Backend

- Enable nullable reference types.
- Treat warnings seriously.
- Use asynchronous APIs for I/O.
- Use dependency injection.
- Keep controllers or endpoints thin.
- Place business and authorization logic in testable services.
- Use Problem Details for API errors.
- Add XML documentation only where it adds real value.

### Frontend

- Use TypeScript strict mode.
- Avoid unsafe HTML rendering.
- Keep authentication logic centralized.
- Do not expose secrets in frontend environment variables.
- Handle unauthorized and expired-session behavior deliberately.

### Infrastructure

- Keep Docker Compose understandable.
- Add health checks where appropriate.
- Avoid privileged containers.
- Do not mount the Docker socket.
- Avoid exposing internal service ports unless development requires them.
- Document any intentionally exposed port.
- Use secure defaults and explicit development overrides.

## Required validation

After relevant changes, run all applicable commands.

Expected eventual commands include:

```powershell
dotnet format --verify-no-changes
dotnet build
dotnet test
npm ci
npm run lint
npm run typecheck
npm run build
docker compose config
docker compose up --build
```

Security phases must additionally run applicable scans:

```powershell
gitleaks detect
semgrep scan
trivy config .
trivy image <image-name>
```

Do not claim a command passed unless it was actually executed successfully.

When a command cannot run:

- State why.
- Include the exact attempted command.
- Include the relevant error.
- Identify what remains unverified.

## Testing expectations

Each phase must include:

- Positive tests.
- Negative tests.
- Authorization tests where applicable.
- Security regression tests.
- Tests for failure paths.
- Tests proving sensitive values are not exposed when feasible.

Do not rely only on mocked authorization for security-critical integration tests.

## Documentation

Update documentation in the same change when behavior, setup, architecture, security controls, or commands change.

Documentation must remain sufficient for a reviewer with only:

- The repository.
- Docker Desktop.
- Git.
- A supported .NET SDK.
- Node.js.

## Git behavior

- Do not rewrite Git history.
- Do not force-push.
- Do not discard unrelated user changes.
- Keep changes scoped to the requested phase.
- Suggest a concise commit message at the end of each completed phase.
- Do not create a commit unless explicitly asked.

## Completion response

At the end of each task, report:

1. What changed.
2. Important security decisions.
3. Files added or modified.
4. Tests and validation commands run.
5. Results.
6. Remaining limitations.
7. The recommended commit message.

Do not report future phases as completed.
