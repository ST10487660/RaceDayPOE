# RaceDay — Full-Stack Event Management System

RaceDay is a full-stack event management system for South African running, walking and cycling events. It allows organisers to manage sporting events, categories, participant enrolments and results, while participants can browse events, enrol and track their performance.

The project was developed in three parts:

- **Part 1:** Project planning, database design and API endpoint planning.
- **Part 2:** ASP.NET Core RESTful Web API, database integration and authentication.
- **Part 3:** ASP.NET Core MVC web application that communicates with the Part 2 API.

## Project Structure

```text
RaceDayPOE-GitHub/
│
├── docs/
│   ├── RaceDay_ERD.png
│   ├── RaceDay_API_Endpoint_Plan.md
│   ├── RaceDay_API_Endpoint_Plan.pdf
│   └── RaceDay_Database.sql
│
├── RaceDay-Part2/
│   └── Part2-API/
│       ├── RaceDay.sln
│       ├── src/
│       │   └── RaceDay.Api/
│       └── tests/
│           └── RaceDay.Api.Tests/
│
├── Part3-MVC/
│   ├── RaceDay.Web.sln
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── src/
│       └── RaceDay.Web/
│
└── .github/
    └── workflows/
```

## Part 1 — Planning and Database Design

Part 1 focused on planning the system before application development.

The `docs` folder contains:

- `RaceDay_ERD.png` — Entity Relationship Diagram.
- `RaceDay_API_Endpoint_Plan.md` — REST API endpoint plan.
- `RaceDay_API_Endpoint_Plan.pdf` — PDF version of the endpoint plan.
- `RaceDay_Database.sql` — SQL Server database creation and sample data script.

### Database Design

The database uses Microsoft SQL Server.

Database name: `RaceDayDB`

The database contains eight main entities:

1. Users
2. Events
3. Categories
4. EventCategories
5. Routes
6. Enrolments
7. Results
8. Weather

### Database Setup

Requirements:
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)

Steps:

1. Open SSMS and connect to your SQL Server instance.
2. Open `docs/RaceDay_Database.sql`.
3. Execute the SQL script.
4. Refresh the Databases folder.
5. Expand `RaceDayDB` and check the tables.

The SQL script creates the database structure and inserts sample data.

### Part 1 Video

Unlisted YouTube video:

https://youtu.be/X8XoADVUu0w

---

## Part 2 — RESTful API

Part 2 implements the backend using ASP.NET Core Web API, C#, .NET 8, Entity Framework Core and SQL Server.

The API handles the application's business logic, authentication, database operations and data management.

### Main Features

- User registration and login.
- Session-based authentication.
- Organiser and Participant roles.
- Event management.
- Category management.
- Participant enrolments.
- Event results.
- Routes and weather information.
- Event banner upload endpoint.
- Swagger API documentation.
- Automated tests using xUnit.

### Authentication and Security

The API uses ASP.NET Core cookie authentication and role-based authorisation.

- Organisers can manage their own events.
- Participants can browse and enrol in events.
- Passwords are securely hashed.
- Unauthenticated requests receive `401 Unauthorized`.
- Users without the required permissions receive `403 Forbidden`.

Organiser registration requires an invitation code.

### Part 2 Setup

Requirements:

- .NET 8 SDK
- Microsoft SQL Server
- SSMS
- Visual Studio 2022 or .NET CLI

1. Open `docs/RaceDay_Database.sql` in SSMS.
2. Execute the script to create the database.
3. Open `RaceDay-Part2/Part2-API/src/RaceDay.Api/appsettings.json`.
4. Update the `RaceDayDb` connection string to match your SQL Server instance.
5. Open a terminal in the repository root.

Run the API:

```bash
dotnet run --project RaceDay-Part2/Part2-API/src/RaceDay.Api --launch-profile http
```

Swagger:

http://localhost:5080/swagger

### Running API Tests

```bash
dotnet test RaceDay-Part2/Part2-API/RaceDay.sln
```

The tests use an in-memory database.

### Demo Accounts

Password: `Password123!`

| Email | Role |
|---|---|
| naledi.organiser@raceday.co.za | Organiser |
| thabo.organiser@raceday.co.za | Organiser |
| lerato.participant@raceday.co.za | Participant |
| kabelo.participant@raceday.co.za | Participant |

Local organiser invitation code: `RACEDAY-ORGANISER-2026`.

### Part 2 Video

Unlisted YouTube video:

https://youtu.be/yPW2g5p8Hhs

---

## Part 3 — ASP.NET Core MVC Web Application

Part 3 introduces the MVC web application that provides the user interface for RaceDay.

The MVC application communicates with the Part 2 REST API through HTTP requests. It does not directly access the SQL Server database.

### Main Features

**Authentication**
- User registration and login.
- Session-based authentication.
- Role-based navigation.
- Protected pages.
- Logout functionality.

**Organiser functionality**
- Organiser dashboard.
- Create, view, edit and delete events.
- Manage event categories.
- View participant enrolments.
- Manage event results.
- Upload event banner images.

**Participant functionality**
- Browse available events.
- View event details and categories.
- Enrol in events.
- View personal event enrolments.
- Track enrolment status.
- View personal results.
- Update profile information.

### MVC and API Integration

The MVC application uses an API client to communicate with the backend.

The general architecture is:

```text
User
  |
  v
Part 3 - ASP.NET Core MVC
  |
  | HTTP Requests
  v
Part 2 - ASP.NET Core Web API
  |
  v
Entity Framework Core
  |
  v
SQL Server Database
```

This separates the presentation layer from the backend business logic and database operations.

### Running Part 3 Locally

Requirements:

- .NET 8 SDK
- Visual Studio 2022 or .NET CLI
- SQL Server
- A configured and running Part 2 API

First, start the Part 2 API using the instructions above.

Then open another terminal from the repository root and run:

```bash
dotnet run --project Part3-MVC/src/RaceDay.Web
```

The MVC application should be available at:

http://localhost:5090

The MVC application uses the API base URL configured in its `appsettings.json`. By default, the local configuration connects to:

`http://localhost:5080`

Both applications must be running for the full system to work.

---

## Event Banner Upload

Organisers can upload banner images for their events through the MVC application.

The MVC application sends the uploaded image to the Part 2 API using a multipart HTTP request.

The API validates the uploaded file and stores the image in its local `wwwroot/uploads/banners` directory. The resulting image URL is saved with the event and displayed in the MVC application.

**Storage note:** The current implementation uses local file storage. Azure Blob Storage integration is not included in this implementation.

---

## Docker

The Part 3 MVC project includes:

- `Part3-MVC/Dockerfile`
- `Part3-MVC/docker-compose.yml`

These files provide Docker configuration for containerising the MVC application.

Docker Desktop and Docker Compose are required.

From the repository root, the intended Compose command is:

```bash
docker compose -f Part3-MVC/docker-compose.yml up --build
```

The container must be configured to communicate with the API using a reachable API address rather than assuming that `localhost` inside the container refers to the host computer.

To stop the containers:

```bash
docker compose -f Part3-MVC/docker-compose.yml down
```

---

## GitHub Actions and Continuous Integration

GitHub Actions is used to automate build validation.

The workflows are stored in:

`.github/workflows/`

The Part 2 workflow is:

`.github/workflows/part2-ci.yml`

The repository also contains the Part 1 validation workflow:

`.github/workflows/validate-part1.yml`

These workflows support automated repository and application validation.

---

## Technologies Used

| Technology | Purpose |
|---|---|
| C# | Application development |
| .NET 8 | Application framework |
| ASP.NET Core Web API | Backend REST API |
| ASP.NET Core MVC | Web application |
| Entity Framework Core | Database access |
| Microsoft SQL Server | Relational database |
| SSMS | Database management |
| Swagger | API documentation and testing |
| xUnit | Automated testing |
| HTML, CSS and JavaScript | Web interface |
| Git and GitHub | Version control |
| GitHub Actions | Continuous integration |
| Docker | Application containerisation |

---

## Video Presentations

**Part 1 — Planning and Database Design**

https://youtu.be/X8XoADVUu0w

**Part 2 — RESTful API**

https://youtu.be/yPW2g5p8Hhs

**Part 3 — MVC Application**

[Insert your unlisted Part 3 YouTube video link here.]

---

## Conclusion

RaceDay was developed as a full-stack event management system. Part 1 established the system requirements and database design, Part 2 implemented the backend REST API, and Part 3 introduced the MVC web application.

Together, these components provide functionality for organisers and participants to manage and participate in South African sporting events.
