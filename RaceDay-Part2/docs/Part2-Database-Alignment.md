# RaceDay Part 2 database alignment

Part 2 retains the eight Part 1 entities: `Users`, `Events`, `Categories`, `EventCategories`, `Routes`, `Enrolments`, `Results`, and `Weather`.

The Part 2 database script adds two required columns to `Events`:
- `EventType` (`Run`, `Walk`, or `Cycle`)
- `DistanceKm`

The Part 2 SQL script retains the Part 1 validation rules for user email and role, event name and location, and category name, while adding event type and distance validation. The Entity Framework configuration reflects these constraints.

Update the ERD in this folder to show `EventType` and `DistanceKm` on `Events` so the documentation matches the SQL schema. Keep the endpoint plan versioned under `/api/v1/`.

## SSMS workflow

1. Open `RaceDay_Database.sql` in SSMS.
2. Back up any existing data first: the script drops and recreates RaceDay tables.
3. Execute it and verify all tables and sample rows.
4. Set `ConnectionStrings:RaceDayDb` in `Part2-API/src/RaceDay.Api/appsettings.json` to the same SQL Server instance used in SSMS.
5. Start the API and test it through Swagger.

The API updates only the known `PART1-DEMO-HASH-...` placeholder password hashes to real ASP.NET Core password hashes at startup. It leaves existing non-placeholder password hashes unchanged.
