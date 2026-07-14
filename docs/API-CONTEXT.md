# Sm_API — Context for building the React frontend

This file is a self-contained reference for implementing a React UI against
the Sm_API backend. It documents the API surface, exact JSON shapes, error
behavior, and relationships needed to build CRUD screens without having to
read the C# source.

## Stack / how to run the API

- .NET 10 ASP.NET Core Web API, controller-based, `src/Sm_API.Api`
- EF Core + SQLite (`smapi.db`, auto-migrated on startup)
- JSON casing: **camelCase** (ASP.NET Core default `System.Text.Json` policy)
- Run locally: `dotnet run --project src/Sm_API.Api` → default profile listens
  on `https://localhost:7127` and `http://localhost:5140` (see
  `src/Sm_API.Api/Properties/launchSettings.json`; confirm against console
  output since this can change)
- OpenAPI JSON (machine-readable spec) at `/openapi/v1.json` in Development —
  useful for generating a typed client (e.g. `openapi-typescript`,
  `orval`, or `openapi-generator-cli`) instead of hand-writing fetch calls.

### CORS

The API has a CORS policy (`Program.cs`) allowing browser requests from:
- `http://localhost:5173` (Vite default)
- `http://localhost:3000` (CRA/Next default)

Configurable via `Cors:AllowedOrigins` in `appsettings.json` — add your dev
server's origin there if it differs. All methods and headers are allowed for
those origins; no credentials/cookies are used (the API has no auth yet).

### Authentication

**None currently implemented.** All endpoints are open. If the React app
needs to demo/enforce auth, that will need to be added to the API first
(not in scope of what's built so far).

## Domain model & relationships

```
Teacher 1 ──< ClassRoom >── * Enrollment >── 1 Student
```

- A `Teacher` has many `ClassRoom`s (one teacher per classroom).
- A `ClassRoom` has many `Enrollment`s.
- A `Student` has many `Enrollment`s.
- `Enrollment` is the join entity between `Student` and `ClassRoom` (a
  student can be enrolled in many classrooms; a classroom has many students).
- A `(StudentId, ClassRoomId)` pair must be unique — a student can't be
  enrolled in the same class twice (enforced by a unique DB index, returns
  `409 Conflict` on violation).

### Field reference (JSON shapes, camelCase)

**Student**
| Field | Type | Notes |
|---|---|---|
| `id` | number | server-generated, omit on create |
| `firstName` | string | required, max 100 |
| `lastName` | string | required, max 100 |
| `email` | string | required, valid email format, max 200, **unique** |
| `dateOfBirth` | string (`YYYY-MM-DD`) | `DateOnly` on the server |
| `enrollmentDate` | string (`YYYY-MM-DD`) | `DateOnly` on the server |

**Teacher**
| Field | Type | Notes |
|---|---|---|
| `id` | number | server-generated |
| `firstName` | string | required, max 100 |
| `lastName` | string | required, max 100 |
| `email` | string | required, valid email, max 200, **unique** |
| `subject` | string | required, max 100 |
| `hireDate` | string (`YYYY-MM-DD`) | |

**ClassRoom**
| Field | Type | Notes |
|---|---|---|
| `id` | number | server-generated |
| `name` | string | required, max 100 |
| `gradeLevel` | number | required, 1–12 |
| `roomNumber` | string | required, max 20 |
| `teacherId` | number | required, must reference an existing Teacher |

**Enrollment**
| Field | Type | Notes |
|---|---|---|
| `id` | number | server-generated |
| `studentId` | number | required, must reference an existing Student |
| `classRoomId` | number | required, must reference an existing ClassRoom |
| `enrollmentDate` | string (`YYYY-MM-DD`) | |

Note: **write** payloads (POST/PUT) never include `id` — it's server-assigned.
**Read** responses always include `id`. There's no separate "read includes
nested objects" behavior — reads are flat (e.g. a ClassRoom read returns
`teacherId`, not a nested `teacher` object). If the UI needs the teacher's
name next to a classroom, fetch `/api/teachers` separately and join client-side,
or fetch by id.

## Endpoints

All four resources follow the identical REST shape. Base path: `/api/{resource}`.
Resources: `students`, `teachers`, `classrooms`, `enrollments`.

| Method | Path | Body | Success | Notes |
|---|---|---|---|---|
| GET | `/api/{resource}` | — | `200 OK`, array of Read DTOs | |
| GET | `/api/{resource}/{id}` | — | `200 OK`, single Read DTO | `404` if not found |
| POST | `/api/{resource}` | Write DTO (JSON) | `201 Created`, Read DTO, `Location` header | `400` on validation error, `409` on conflict (see below) |
| PUT | `/api/{resource}/{id}` | Write DTO (JSON) | `204 No Content` | `404` if not found, `400`/`409` as above |
| DELETE | `/api/{resource}/{id}` | — | `204 No Content` | `404` if not found; see delete guards below |

### Example: create a student

```
POST /api/students
Content-Type: application/json

{
  "firstName": "Ada",
  "lastName": "Lovelace",
  "email": "ada@school.test",
  "dateOfBirth": "2012-05-01",
  "enrollmentDate": "2024-09-01"
}
```

Response `201 Created`:
```json
{
  "id": 1,
  "firstName": "Ada",
  "lastName": "Lovelace",
  "email": "ada@school.test",
  "dateOfBirth": "2012-05-01",
  "enrollmentDate": "2024-09-01"
}
```

### Error responses

Errors use RFC 7807 `ProblemDetails` shape, e.g.:

```json
{
  "title": "A student with this email already exists.",
  "status": 409
}
```

Validation errors (`400`) from ASP.NET Core's automatic model validation come
back as the standard ASP.NET `ValidationProblemDetails` shape instead, with
an `errors` dictionary keyed by field name:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  }
}
```

### Known 409/400 business rules the UI should handle gracefully

- Creating/updating a **Student** or **Teacher** with an email that's already
  in use → `409`.
- Creating a **ClassRoom** with a `teacherId` that doesn't exist → `400`.
- Creating an **Enrollment** with a `studentId` or `classRoomId` that doesn't
  exist → `400`.
- Creating a duplicate **Enrollment** (same student + classroom already
  enrolled) → `409`.
- Deleting a **Teacher** who still has classrooms assigned → `409` (delete
  or reassign the classrooms first).
- Deleting a **Student** or **ClassRoom** cascades and removes their
  **Enrollments** automatically (no guard, unlike Teacher).

## Suggested React app shape

A reasonable structure mirroring the four resources:

```
src/
  api/
    client.ts          # fetch wrapper, base URL from env (VITE_API_BASE_URL)
    students.ts         # typed CRUD calls for /api/students
    teachers.ts
    classrooms.ts
    enrollments.ts
  types/
    models.ts           # Student, Teacher, ClassRoom, Enrollment, *WriteDto types
  pages/
    students/  (List, Detail/Form)
    teachers/  (List, Detail/Form)
    classrooms/ (List, Detail/Form — needs a Teacher picker, so fetch teachers list too)
    enrollments/ (List, Form — needs Student + ClassRoom pickers)
  components/
    DataTable, FormField, ConflictErrorBanner (for surfacing 409s), etc.
```

Recommended: generate `types/models.ts` and the fetch client directly from
`/openapi/v1.json` rather than hand-transcribing this table, to avoid drift
as the API evolves — but the table above is accurate as of this API version
and sufficient to hand-write a client if preferred.

## Verifying frontend integration

1. `dotnet run --project src/Sm_API.Api` (note the printed URL)
2. Point the React app's API base URL at it (respecting the CORS origins above)
3. Exercise the golden path: create a Teacher → create a ClassRoom for that
   Teacher → create a Student → create an Enrollment linking them → confirm
   list/detail/edit/delete all round-trip correctly, and that a duplicate
   enrollment or unknown foreign key surfaces the `400`/`409` from the API
   rather than crashing the UI.
