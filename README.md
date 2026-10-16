# RaceDay — Part 1

RaceDay is a full-stack event management system for South African running, walking and cycling events. The system allows event organisers to manage events, categories, enrolments and participant results, while participants can browse events, enter events and track their performance.

Part 1 focuses on planning and database design before application code is written. No API code is implemented in Part 1.

## Part 1 Deliverables

The `/docs` folder contains:

- `RaceDay_ERD.png` — Entity Relationship Diagram showing the RaceDay database structure and relationships.
- `RaceDay_API_Endpoint_Plan.md` — REST API endpoint plan for the API that will be implemented in Part 2.
- `RaceDay_API_Endpoint_Plan.pdf` — PDF version of the API endpoint plan.
- `RaceDay_Database.sql` — SQL Server database creation and sample data script.

## User Roles

### Organiser

The Organiser is responsible for managing RaceDay events. An Organiser can:

- Create, edit and delete events.
- Manage event categories.
- View event enrolments.
- Capture and update participant results.

### Participant

The Participant uses RaceDay to find and enter events. A Participant can:

- Register and log in.
- Browse available events.
- Enter an event by selecting a category.
- View their own event enrolments.
- Track their personal results.

## Database Design

The RaceDay database was designed using Microsoft SQL Server.

Database name: `RaceDayDB`

The database contains eight entities:

1. Users
2. Events
3. Categories
4. EventCategories
5. Routes
6. Enrolments
7. Results
8. Weather

The SQL script includes primary keys, foreign keys and database constraints. It also inserts sample data including two organisers, two participants, three events, event categories, enrolments, results, routes and weather information.

## Database Setup Instructions

### Requirements

Before running the database script, install:

- Microsoft SQL Server
- SQL Server Management Studio (SSMS)

### Running the Database

1. Clone or download the RaceDay repository.
2. Open SQL Server Management Studio.
3. Connect to a SQL Server instance.
4. Open `docs/RaceDay_Database.sql`.
5. Click **Execute** to run the complete script.
6. The script will create the `RaceDayDB` database and its required tables.
7. After execution, refresh the **Databases** folder in Object Explorer.
8. Expand `RaceDayDB` and then **Tables** to view the created tables.
9. The verification queries at the end of the script display the sample records inserted into the database.

The SQL script has been designed so that it can be re-run during testing by dropping the RaceDay tables in dependency order before recreating them.

## API Planning

No REST API code is implemented in Part 1.

The planned REST API endpoints are documented in:

`docs/RaceDay_API_Endpoint_Plan.md`

The endpoint plan covers:

- Authentication
- User profiles
- Events
- Categories
- Event enrolments
- Results
- Routes
- Weather information

The Part 2 C# REST API will follow this endpoint plan. Any deliberate changes made during Part 2 will be documented.

## GitHub Actions

GitHub Actions is used to validate the Part 1 repository structure.

The workflow is located at:

`.github/workflows/validate-part1.yml`

The workflow checks that the README and required Part 1 files are present in the repository.

## CI/CD Screenshot

<img width="1002" height="464" alt="Successful RaceDay Part 1 GitHub Actions build" src="https://github.com/user-attachments/assets/a915da3a-146c-4c58-9a76-14d4bd9371ac" />

The screenshot above shows the successful GitHub Actions validation with a green build.

## Video Presentation

The Part 1 video presentation demonstrates and explains:

1. The purpose of RaceDay.
2. The Organiser and Participant roles.
3. The Entity Relationship Diagram.
4. The REST API endpoint plan.
5. The SQL Server database design and sample data.
6. The GitHub repository structure.
7. The successful GitHub Actions workflow.

YouTube video (unlisted):

https://youtu.be/X8XoADVUu0w


# RaceDay — Part 2: RESTful API

The Part 2 solution lives in the [`Part2-API`](Part2-API) folder. It is an ASP.NET Core Web API (C#, .NET 8) using Entity Framework Core (Code-First) with SQL Server, session-based authentication with two roles, Swagger, xUnit tests and a GitHub Actions CI workflow.

```
Part2-API/
  RaceDay.sln
  src/RaceDay.Api/            the API (Controllers, Models, Dtos, Data, Mapping, Infrastructure)
  tests/RaceDay.Api.Tests/    xUnit integration-style unit tests
.github/workflows/part2-ci.yml  builds and tests on every push
```

## Setup and run instructions

**Requirements:** .NET 8 SDK, Microsoft SQL Server, SQL Server Management Studio (SSMS), and Visual Studio 2022 or the `dotnet` CLI.

### 1. Create the database in SSMS

1. Open SSMS and connect to the SQL Server instance you intend to use.
2. Open `docs/RaceDay_Database.sql` from this repository.
3. **Important:** this script drops and recreates the RaceDay tables. Back up any data you need before executing it.
4. Execute the script and confirm it completes without errors.
5. Refresh `RaceDayDB` in Object Explorer and check that the eight tables exist.

The Part 2 database script includes the Part 1 schema plus `EventType` and `DistanceKm` columns in `Events`. Keep the ERD and endpoint documentation consistent with these changes.

### 2. Point the API to the same SQL Server instance

Open `Part2-API/src/RaceDay.Api/appsettings.json` and update `ConnectionStrings:RaceDayDb` so `Server=...` matches the server name you used in SSMS. The default connection string targets LocalDB: `(localdb)\MSSQLLocalDB`. If you connected to a different SQL Server instance in SSMS, replace that server value. Keep `Database=RaceDayDB` and the remaining options unless your setup requires otherwise.

The API uses Entity Framework Core with the existing SQL schema. When it starts, it will not overwrite existing table data. If the database contains the sample placeholder password hashes from the SQL script, the API replaces those specific demo placeholders with usable password hashes; it does not replace existing non-placeholder password hashes.

### 3. Run the API

From the repository root, run:

```bash
dotnet run --project Part2-API/src/RaceDay.Api --launch-profile http
```

Swagger UI should be available at **http://localhost:5080/swagger**. Use `POST /api/v1/auth/login` to log in before testing protected endpoints.

**Demo accounts (password `Password123!`):** `naledi.organiser@raceday.co.za` (Organiser), `thabo.organiser@raceday.co.za` (Organiser), `lerato.participant@raceday.co.za` (Participant), `kabelo.participant@raceday.co.za` (Participant).

### 4. Run the tests

Run:

```bash
dotnet test Part2-API/RaceDay.sln
```

The tests use an in-memory database, so they do not require your SSMS database.

### Trying protected endpoints in Swagger
Call `POST /api/v1/auth/login` first. The API sets a `RaceDay.Session` cookie and Swagger UI sends it automatically on every later request. Use `POST /api/v1/auth/logout` to end the session and try the other role.

## The two roles

| | Organiser | Participant |
|---|---|---|
| Register / log in / view and update own profile | Yes | Yes |
| View events, categories, route and weather | Yes | Yes |
| Create, update, delete events (own events only) | **Yes** | No (403) |
| Create, update, delete categories | **Yes** | No (403) |
| View all enrolments for own events | **Yes** | No (403) |
| Capture, correct, delete results for own events | **Yes** | No (403) |
| Enrol in an event by choosing a category, cancel own enrolment | No (403) | **Yes** |
| View own enrolments and own results | No (403) | **Yes** |

Requests with no session receive `401 Unauthorized`; a logged-in user using the wrong role receives `403 Forbidden`.

### How roles are managed at registration
Anyone may register as a **Participant**. Registering as an **Organiser** also requires a secret **invite code** (`Registration:OrganiserInviteCode` in configuration). Without a valid code the API returns `403`. This stops people from simply choosing "Organiser" and gaining control over events and results. In production the code would be supplied through an environment variable or user-secrets rather than being committed.
Local invite code for marking/demo purposes: `RACEDAY-ORGANISER-2026`.

## Authentication and security
- **Session management:** ASP.NET Core cookie authentication. After login the server issues an encrypted, `HttpOnly`, `SameSite=Lax` session cookie holding the user id and role, which is checked on every request (60 minute sliding expiry).
- **Passwords** are hashed with ASP.NET Core's `PasswordHasher` (PBKDF2 with a per-user salt). The original password is never stored.
- **Role-based authorisation** uses `[Authorize(Roles = ...)]`. Organisers can additionally only modify data on events they own.
- Errors use a consistent JSON body `{ "statusCode": 400, "message": "..." }` without exposing internal details.

## Deliberate changes to the Part 1 plan
Part 1 promised that any deliberate changes would be documented. They are:

| Change | Reason |
|---|---|
| Session cookie instead of a returned access token. Added `POST /api/v1/auth/logout`. | The Part 2 brief requires session management. |
| `POST /auth/register` accepts a `role` (and `organiserInviteCode` for Organisers) instead of always creating a Participant. | The brief requires users to choose their role; the invite code controls Organiser access. |
| `Events` gained `EventType` (Run/Walk/Cycle) and `DistanceKm` columns; `docs/RaceDay_Database.sql` was updated to match. **The ERD in `docs` must show these two columns too.** | The brief requires each event to capture distance and event type. |
| All routes use the `/api/v1/` prefix (the access-control section of the plan mentioned `/api/users/me`). | Consistency with the versioning rule in the plan. |
| `DELETE /enrolments/{id}` marks the enrolment `Cancelled` rather than deleting the row; a cancelled participant may enrol again. | Keeps history and respects the unique (participant, event) constraint. |
| Organisers can only modify, and view enrolments/results for, **their own** events. | "Organisers must be able to view enrolments for *their* events." |
| `POST /events/{id}/results` requires both `position` and `finishTime`. | The brief requires finish times and finishing positions to be captured. |
| `GET /events` accepts an optional `includePast=true` query parameter. | Convenience; the default still lists upcoming events only. |

## Unit tests
The tests (`Part2-API/tests/RaceDay.Api.Tests`) start the real API in memory with an in-memory EF Core database and make real HTTP calls. They cover: registration and login (success, duplicates, validation, hashed password, invite code), event management (create/update/delete, ownership), role rejection (403) and missing session (401) for every protected endpoint, enrolments (success, recorded correctly, duplicates, full events, cancellation), results, and a Swagger test that checks every endpoint from the Part 1 plan exists in the generated API document.

## GitHub Actions (Part 2)
`.github/workflows/part2-ci.yml` restores, builds and runs all tests on every push.

### CI/CD screenshot (Part 2)
<!-- TODO: after the first green run, drag a screenshot of the successful Actions run here -->
_Screenshot of the successful Part 2 build goes here._

## Part 2 video (unlisted YouTube)
<!-- TODO: paste your unlisted YouTube link -->
_YouTube link goes here._


