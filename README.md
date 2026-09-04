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

Replace the placeholder below with a screenshot of your successful GitHub Actions green build before final submission.

`[INSERT YOUR GITHUB ACTIONS GREEN BUILD SCREENSHOT HERE]`

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

> Do not use an AI-generated voice for the submission video. Use your own voice as required by the assessment.

## Suggested Commit Plan

The assessment requires a minimum of 20 meaningful commits for Part 1. Make these commits from your own GitHub account as you complete real work. Do not create empty or fake commits.

Example progression:

1. `Initial RaceDay repository structure`
2. `Add Part 1 documentation folder`
3. `Create initial ERD entities`
4. `Add Users entity and role design`
5. `Add Events entity`
6. `Add Categories entity`
7. `Add EventCategories relationship`
8. `Add Enrolments entity`
9. `Add Results entity`
10. `Add Routes entity`
11. `Add Weather entity`
12. `Complete ERD relationships`
13. `Add authentication endpoints to plan`
14. `Add event endpoints to plan`
15. `Add category endpoints to plan`
16. `Add enrolment endpoints to plan`
17. `Add results endpoints to plan`
18. `Create SQL schema`
19. `Add SQL constraints and seed data`
20. `Add README and GitHub Actions validation`

Each commit should represent a genuine change.
