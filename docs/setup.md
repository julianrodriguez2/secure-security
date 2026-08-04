# Setup guide

## Phase 0

Install the prerequisites listed in the repository README, then run:

    dotnet restore
    dotnet format --verify-no-changes
    dotnet build
    dotnet test
    npm ci --prefix src/SecureNotes.Web
    npm run lint --prefix src/SecureNotes.Web
    npm run typecheck --prefix src/SecureNotes.Web
    npm run build --prefix src/SecureNotes.Web

The API is an empty scaffold and intentionally exposes no application endpoints. Docker Desktop, local certificates, Keycloak, PostgreSQL, Nginx, and observability services are introduced in later phases.

