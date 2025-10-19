using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EventManagement.Infrastructure
{
    /* This lets dotnet ef know how to build 
     * your DbContext at design time — even without running your API. /
     */
    public class EventManagementDbContextFactory
        : IDesignTimeDbContextFactory<EventManagementDbContext>
    {
        public EventManagementDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "../EventManagement.Api")
                    )
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Get connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configure DbContext
            var optionsBuilder = new DbContextOptionsBuilder<EventManagementDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new EventManagementDbContext(optionsBuilder.Options);
        }
    }
}
