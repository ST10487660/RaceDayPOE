using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "RaceDayTests-" + Guid.NewGuid();

    /// <summary>The invite code the API expects for Organiser registration.</summary>
    public string OrganiserInviteCode =>
        Services.GetRequiredService<IConfiguration>()["Registration:OrganiserInviteCode"]!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration (works across EF Core versions).
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration") &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(RaceDayDbContext))).ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_databaseName));
        });
    }
}
