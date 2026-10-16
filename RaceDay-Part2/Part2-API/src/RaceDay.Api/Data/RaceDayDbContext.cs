using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Code-first model. Table names, column sizes, unique indexes and check constraints
/// mirror docs/RaceDay_Database.sql from Part 1.
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<EventRoute> Routes => Set<EventRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();
    public DbSet<WeatherRecord> WeatherRecords => Set<WeatherRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users", t =>
            {
                t.HasCheckConstraint("CK_Users_Role", "[Role] IN (N'Organiser', N'Participant')");
                t.HasCheckConstraint("CK_Users_Email", "[Email] LIKE '%_@_%._%'");
            });
            b.HasKey(u => u.UserId);
            b.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            b.Property(u => u.Email).HasMaxLength(150).IsRequired();
            b.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            b.Property(u => u.Role).HasMaxLength(20).IsRequired();
            b.Property(u => u.CreatedAt).HasColumnType("datetime2(0)");
        });

        modelBuilder.Entity<Event>(b =>
        {
            b.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_MaxParticipants", "[MaxParticipants] > 0");
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN (N'Run', N'Walk', N'Cycle')");
                t.HasCheckConstraint("CK_Events_DistanceKm", "[DistanceKm] > 0");
                t.HasCheckConstraint("CK_Events_EventName", "LEN(LTRIM(RTRIM([EventName]))) > 0");
                t.HasCheckConstraint("CK_Events_Location", "LEN(LTRIM(RTRIM([Location]))) > 0");
            });
            b.HasKey(e => e.EventId);
            b.Property(e => e.EventName).HasMaxLength(150).IsRequired();
            b.Property(e => e.Description).HasMaxLength(500);
            b.Property(e => e.EventDate).HasColumnType("date");
            b.Property(e => e.Location).HasMaxLength(200).IsRequired();
            b.Property(e => e.EventType).HasMaxLength(20).IsRequired();
            b.Property(e => e.DistanceKm).HasPrecision(6, 2);
            b.Property(e => e.CreatedAt).HasColumnType("datetime2(0)");
            b.HasOne(e => e.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(e => e.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("Categories", t => t.HasCheckConstraint("CK_Categories_Name", "LEN(LTRIM(RTRIM([CategoryName]))) > 0"));
            b.HasKey(c => c.CategoryId);
            b.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            b.HasIndex(c => c.CategoryName).IsUnique().HasDatabaseName("UQ_Categories_Name");
            b.Property(c => c.Description).HasMaxLength(300);
        });

        modelBuilder.Entity<EventCategory>(b =>
        {
            b.ToTable("EventCategories");
            b.HasKey(ec => new { ec.EventId, ec.CategoryId });
            b.HasOne(ec => ec.Event).WithMany(e => e.EventCategories)
                .HasForeignKey(ec => ec.EventId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(ec => ec.Category).WithMany(c => c.EventCategories)
                .HasForeignKey(ec => ec.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventRoute>(b =>
        {
            b.ToTable("Routes", t => t.HasCheckConstraint("CK_Routes_Distance", "[DistanceKm] > 0"));
            b.HasKey(r => r.RouteId);
            b.Property(r => r.RouteName).HasMaxLength(150).IsRequired();
            b.Property(r => r.DistanceKm).HasPrecision(6, 2);
            b.Property(r => r.StartPoint).HasMaxLength(200).IsRequired();
            b.Property(r => r.EndPoint).HasMaxLength(200).IsRequired();
            b.HasOne(r => r.Event).WithOne(e => e.Route)
                .HasForeignKey<EventRoute>(r => r.EventId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(b =>
        {
            b.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status",
                "[Status] IN (N'Pending', N'Confirmed', N'Cancelled')"));
            b.HasKey(e => e.EnrolmentId);
            b.Property(e => e.Status).HasMaxLength(20).IsRequired();
            b.Property(e => e.EnrolmentDate).HasColumnType("datetime2(0)");
            b.HasIndex(e => new { e.ParticipantId, e.EventId }).IsUnique()
                .HasDatabaseName("UQ_Enrolments_Participant_Event");
            b.HasOne(e => e.EventCategory).WithMany(ec => ec.Enrolments)
                .HasForeignKey(e => new { e.EventId, e.CategoryId }).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RaceResult>(b =>
        {
            b.ToTable("Results", t =>
            {
                t.HasCheckConstraint("CK_Results_Position", "[Position] IS NULL OR [Position] > 0");
                t.HasCheckConstraint("CK_Results_Distance", "[DistanceKm] IS NULL OR [DistanceKm] > 0");
            });
            b.HasKey(r => r.ResultId);
            b.Property(r => r.FinishTime).HasColumnType("time(0)");
            b.Property(r => r.DistanceKm).HasPrecision(6, 2);
            b.Property(r => r.RecordedAt).HasColumnType("datetime2(0)");
            b.HasOne(r => r.Enrolment).WithOne(e => e.Result)
                .HasForeignKey<RaceResult>(r => r.EnrolmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WeatherRecord>(b =>
        {
            b.ToTable("Weather", t => t.HasCheckConstraint("CK_Weather_Wind", "[WindSpeedKph] IS NULL OR [WindSpeedKph] >= 0"));
            b.HasKey(w => w.WeatherId);
            b.Property(w => w.Conditions).HasMaxLength(100);
            b.Property(w => w.TemperatureC).HasPrecision(5, 2);
            b.Property(w => w.WindSpeedKph).HasPrecision(6, 2);
            b.Property(w => w.RecordedAt).HasColumnType("datetime2(0)");
            b.HasOne(w => w.Event).WithMany(e => e.WeatherRecords)
                .HasForeignKey(w => w.EventId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
