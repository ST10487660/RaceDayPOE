using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Runs once when the application starts (and NOT when `dotnet ef` inspects the project):
/// applies migrations (or creates the schema if no migration exists yet) and seeds demo data.
/// </summary>
public class DatabaseInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _environment;

    public DatabaseInitializer(IServiceScopeFactory scopeFactory, IConfiguration config, IHostEnvironment environment)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        if (db.Database.IsRelational() && db.Database.GetMigrations().Any())
            db.Database.Migrate();       // normal path once `dotnet ef migrations add InitialCreate` has been run
        else
            db.Database.EnsureCreated(); // fallback before any migration exists (and for in-memory tests)

        if (!_environment.IsEnvironment("Testing") && _config.GetValue<bool>("Seed:Enabled"))
            DbSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
