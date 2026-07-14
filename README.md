# Sm_API — School Management System API

A .NET 10 Web API with full CRUD operations for a simple school management
domain (students, teachers, classrooms, enrollments), plus a hardened GitHub
Actions pipeline covering secret scanning, dependency vulnerability scanning,
and a repo security-compliance audit.

## Solution layout

```
src/Sm_API.Api/     ASP.NET Core Web API (controllers, EF Core + SQLite)
tests/Sm_API.Tests/ xUnit + WebApplicationFactory integration tests
.github/            CI, gitleaks, and compliance-audit workflows + Dependabot
scripts/            One-time gh-CLI repo hardening script
docs/                Security hardening reference (see docs/SECURITY-HARDENING.md)
```

## Domain model

- **Student** — Id, FirstName, LastName, Email (unique), DateOfBirth, EnrollmentDate
- **Teacher** — Id, FirstName, LastName, Email (unique), Subject, HireDate
- **ClassRoom** — Id, Name, GradeLevel, RoomNumber, TeacherId (FK)
- **Enrollment** — Id, StudentId (FK), ClassRoomId (FK), EnrollmentDate — join between Student and ClassRoom

Each entity has a controller exposing:

```
GET    /api/{resource}
GET    /api/{resource}/{id}
POST   /api/{resource}
PUT    /api/{resource}/{id}
DELETE /api/{resource}/{id}
```

Resources: `students`, `teachers`, `classrooms`, `enrollments`.

## Running locally

```bash
dotnet restore
dotnet build
dotnet run --project src/Sm_API.Api
```

The API applies EF Core migrations automatically on startup and uses a local
SQLite file (`smapi.db`, gitignored) by default. In development, OpenAPI JSON
is available at `/openapi/v1.json`.

## Running tests

```bash
dotnet test
```

Integration tests spin up the full app via `WebApplicationFactory<Program>`
against a real in-memory SQLite connection (not the EF InMemory provider), so
constraints and cascade behavior match production.

## Security hardening

See [docs/SECURITY-HARDENING.md](docs/SECURITY-HARDENING.md) for the full
mapping of branch protection / PR reviews / signed commits / SSO / audit logs
/ secret scanning / dependency scanning to what's automated here vs. what
requires a GitHub org/enterprise owner to configure manually.

After first pushing this repo to GitHub:

```powershell
./scripts/setup-github-security.ps1 -Repo "your-org/Sm_API"
```

## Building a frontend against this API

See [docs/API-CONTEXT.md](docs/API-CONTEXT.md) — a self-contained reference
(endpoints, JSON shapes, validation/error behavior, CORS config) intended to
be handed to whoever (or whatever) builds the React UI, without needing to
read the C# source first.
