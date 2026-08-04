# SecureNotes Security Lab

A local, free cybersecurity portfolio project for demonstrating secure application development and evidence-based security controls.

## Current status

Phase 0 is complete: the repository has a buildable ASP.NET Core and React/TypeScript foundation, placeholder tests, formatting/linting/type-checking, and CI scaffolding.

No application functionality exists yet. Authentication, notes, databases, Docker Compose, Keycloak, Nginx, observability, security scanning, and end-to-end browser tests are intentionally deferred to their planned phases.

## Prerequisites

- Git
- .NET SDK 10.0.110 or a compatible latest .NET 10 patch
- Node.js 22.13.0 or later compatible Node.js LTS release
- npm 10 or later

Docker Desktop is not required until Phase 1.

## Phase 0 validation

Run these commands from the repository root:

    dotnet restore
    dotnet format --verify-no-changes
    dotnet build
    dotnet test
    npm ci --prefix src/SecureNotes.Web
    npm run lint --prefix src/SecureNotes.Web
    npm run typecheck --prefix src/SecureNotes.Web
    npm run build --prefix src/SecureNotes.Web
    git status

The API project is an intentionally empty host and has no public endpoints. The web project is a static Phase 0 scaffold only.

## Local configuration

Copy .env.example to .env only when a later phase requires local configuration. Do not add real credentials, tokens, private keys, or connection strings to any tracked file.

## Project documentation

- docs/PRD.md — product requirements
- docs/IMPLEMENTATION_PLAN.md — phased execution plan
- docs/setup.md — setup notes, expanded as phases are implemented

## Cost statement

SecureNotes Security Lab is designed to run locally using free and open-source tooling. It does not require paid cloud services.
