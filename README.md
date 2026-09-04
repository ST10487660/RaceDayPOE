# RaceDay — Part 1

RaceDay is a full-stack event-management system for South African running, walking and cycling events. Part 1 focuses on planning the system before application code is written.

## Part 1 Deliverables

The `/docs` folder contains:

- `RaceDay_ERD.png` — Entity Relationship Diagram.
- `RaceDay_API_Endpoint_Plan.md` — complete REST API endpoint plan.
- `RaceDay_Database.sql` — SQL Server database creation and seed script.
- `RaceDay_API_Endpoint_Plan.pdf` — PDF version of the endpoint plan, if supplied.

## Roles

### Organiser
- Create, edit and delete events.
- Manage event categories.
- View event enrolments.
- Capture and correct participant results.

### Participant
- Register and log in.
- Browse upcoming events.
- Enter an event by selecting a category.
- View own enrolments.
- View personal result history.

## Database

The SQL script targets Microsoft SQL Server and can be executed in SQL Server Management Studio (SSMS).

Database name: `RaceDayDB`

The script creates eight entities:

1. Users
2. Events
3. Categories
4. EventCategories
5. Routes
6. Enrolments
7. Results
8. Weather

The script also inserts realistic sample data, including at least two organisers, two participants, three events, event categories, enrolments, results and weather records.

## Part 2 Continuity

The Part 2 C# REST API should follow the endpoint plan in `docs/RaceDay_API_Endpoint_Plan.md`. Do not redesign the database or API without documenting any deliberate changes.

## GitHub Actions

The workflow in `.github/workflows/validate-part1.yml` checks that the required Part 1 files exist.

After pushing to GitHub, confirm that the workflow completes successfully with a green check.

## CI/CD Screenshot


## Video Presentation

Record your own voice-over video explaining:

1. The RaceDay problem and purpose.
2. The two user roles.
3. The ERD and relationships.
4. The API endpoint plan.
5. The SQL database design and sample data.
6. How the GitHub repository is structured.

YouTube video (unlisted):

`[PASTE YOUR UNLISTED YOUTUBE LINK HERE]`



