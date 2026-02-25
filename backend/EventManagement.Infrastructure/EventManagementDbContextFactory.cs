using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EventManagement.Infrastructure;

/// <summary>
/// Design-time DbContext factory for EF Core migrations.
/// Allows running 'dotnet ef' commands without starting the API project.
/// </summary>
public class EventManagementDbContextFactory
    : IDesignTimeDbContextFactory<EventManagementDbContext>
{
    public EventManagementDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "../EventManagement.Api")
                )
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Get connection string
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        // Configure DbContext
        var optionsBuilder = new DbContextOptionsBuilder<EventManagementDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString);

        return new EventManagementDbContext(optionsBuilder.Options);
    }
}
