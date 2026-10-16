using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Seeds the same sample data as the Part 1 SQL script, but with REAL password hashes
/// so the demo accounts can actually log in. Demo password for all four users: Password123!
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Password123!";

    public static void Seed(RaceDayDbContext db, IPasswordHasher<User> hasher)
    {
        // If the database was created by docs/RaceDay_Database.sql in SSMS,
        // it already contains demo users with placeholder hashes. Replace only
        // those known placeholders with real ASP.NET Core password hashes.
        // Do not overwrite passwords for accounts that already have real hashes.
        if (db.Users.Any())
        {
            var demoUsers = db.Users
                .Where(u => u.PasswordHash.StartsWith("PART1-DEMO-HASH-"))
                .ToList();

            foreach (var user in demoUsers)
                user.PasswordHash = hasher.HashPassword(user, DemoPassword);

            if (demoUsers.Count > 0)
                db.SaveChanges();

            return;
        }

        User MakeUser(string name, string email, string role)
        {
            var user = new User { FullName = name, Email = email, Role = role, CreatedAt = DateTime.UtcNow };
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            return user;
        }

        var naledi = MakeUser("Naledi Mokoena", "naledi.organiser@raceday.co.za", AppRoles.Organiser);
        var thabo = MakeUser("Thabo Dlamini", "thabo.organiser@raceday.co.za", AppRoles.Organiser);
        var lerato = MakeUser("Lerato Molefe", "lerato.participant@raceday.co.za", AppRoles.Participant);
        var kabelo = MakeUser("Kabelo Ndlovu", "kabelo.participant@raceday.co.za", AppRoles.Participant);

        var run10 = new Category { CategoryName = "10 km Run", Description = "Ten kilometre road running category." };
        var half = new Category { CategoryName = "21 km Half Marathon", Description = "Half marathon road running category." };
        var walk5 = new Category { CategoryName = "5 km Community Walk", Description = "Five kilometre community walking category." };
        var cycle40 = new Category { CategoryName = "40 km Cycle", Description = "Forty kilometre community cycling category." };

        var e1c10 = new EventCategory { Category = run10 };
        var e1c5 = new EventCategory { Category = walk5 };
        var e2c21 = new EventCategory { Category = half };
        var e2c10 = new EventCategory { Category = run10 };
        var e3c40 = new EventCategory { Category = cycle40 };

        var now = DateTime.UtcNow;

        var ev1 = new Event
        {
            Organiser = naledi, EventName = "Johannesburg Spring Run",
            Description = "Community road-running event in Johannesburg.",
            EventDate = new DateOnly(2026, 10, 18), Location = "Johannesburg, Gauteng",
            EventType = EventTypes.Run, DistanceKm = 10.00m, MaxParticipants = 1500, CreatedAt = now,
            EventCategories = { e1c10, e1c5 },
            Route = new EventRoute { RouteName = "Johannesburg Spring Loop", DistanceKm = 10.00m, StartPoint = "Emmarentia Dam", EndPoint = "Emmarentia Dam" },
            WeatherRecords = { new WeatherRecord { RecordedAt = now, TemperatureC = 21.50m, Conditions = "Partly cloudy", WindSpeedKph = 14.20m } }
        };
        var ev2 = new Event
        {
            Organiser = naledi, EventName = "Pretoria City Half Marathon",
            Description = "Half-marathon event through Pretoria.",
            EventDate = new DateOnly(2026, 11, 8), Location = "Pretoria, Gauteng",
            EventType = EventTypes.Run, DistanceKm = 21.10m, MaxParticipants = 2000, CreatedAt = now,
            EventCategories = { e2c21, e2c10 },
            Route = new EventRoute { RouteName = "Pretoria City Route", DistanceKm = 21.10m, StartPoint = "Union Buildings", EndPoint = "Union Buildings" },
            WeatherRecords = { new WeatherRecord { RecordedAt = now, TemperatureC = 19.00m, Conditions = "Sunny", WindSpeedKph = 11.80m } }
        };
        var ev3 = new Event
        {
            Organiser = thabo, EventName = "Soweto Community Cycle",
            Description = "Community cycling event supporting local charities.",
            EventDate = new DateOnly(2026, 11, 22), Location = "Soweto, Gauteng",
            EventType = EventTypes.Cycle, DistanceKm = 40.00m, MaxParticipants = 1000, CreatedAt = now,
            EventCategories = { e3c40 },
            Route = new EventRoute { RouteName = "Soweto Charity Loop", DistanceKm = 40.00m, StartPoint = "Orlando Stadium", EndPoint = "Orlando Stadium" },
            WeatherRecords = { new WeatherRecord { RecordedAt = now, TemperatureC = 23.20m, Conditions = "Clear", WindSpeedKph = 9.50m } }
        };

        var en1 = new Enrolment { Participant = lerato, EventCategory = e1c10, EnrolmentDate = now, Status = EnrolmentStatuses.Confirmed };
        var en2 = new Enrolment { Participant = kabelo, EventCategory = e1c5, EnrolmentDate = now, Status = EnrolmentStatuses.Confirmed };
        var en3 = new Enrolment { Participant = lerato, EventCategory = e2c21, EnrolmentDate = now, Status = EnrolmentStatuses.Confirmed };
        var en4 = new Enrolment { Participant = kabelo, EventCategory = e3c40, EnrolmentDate = now, Status = EnrolmentStatuses.Confirmed };

        var r1 = new RaceResult { Enrolment = en1, Position = 18, FinishTime = new TimeSpan(0, 52, 14), DistanceKm = 10.00m, RecordedAt = now };
        var r2 = new RaceResult { Enrolment = en2, Position = 7, FinishTime = new TimeSpan(0, 48, 31), DistanceKm = 5.00m, RecordedAt = now };

        db.Events.AddRange(ev1, ev2, ev3);
        db.Enrolments.AddRange(en1, en2, en3, en4);
        db.Results.AddRange(r1, r2);
        db.SaveChanges();
    }
}
