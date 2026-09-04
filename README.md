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

`[PASTE YOUR UNLISTED YOUTUBE LINK HERE]`


