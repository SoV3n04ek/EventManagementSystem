using EventManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure;

public class EventManagementDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Force all table names to lowercase
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()!.ToLowerInvariant());
        }

        // User -> Event (Organizer)
        _ = modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.OrganizedEvents)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Event <-> Participant <-> User (many-to-many)
        _ = modelBuilder.Entity<Participant>()
            .HasOne(p => p.Event)
            .WithMany(e => e.Participants)
            .HasForeignKey(p => p.EventId);

        _ = modelBuilder.Entity<Participant>()
            .HasOne(p => p.User)
            .WithMany(u => u.Participations)
            .HasForeignKey(p => p.UserId);

        // Optional: unique constraint to prevent duplicate participation
        _ = modelBuilder.Entity<Participant>()
            .HasIndex(p => new { p.EventId, p.UserId })
            .IsUnique();
    }

    /// <summary>
    /// Global safety guard: Normalize all DateTime properties to UTC before save.
    /// PostgreSQL 'timestamp with time zone' requires DateTimeKind.Utc.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.ClrType == typeof(DateTime) && prop.CurrentValue is DateTime dt)
                    {
                        if (dt.Kind == DateTimeKind.Unspecified)
                        {
                            prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        }
                    }
                    else if (prop.Metadata.ClrType == typeof(DateTime?) && prop.CurrentValue is DateTime ndt)
                    {
                        if (ndt.Kind == DateTimeKind.Unspecified)
                        {
                            prop.CurrentValue = DateTime.SpecifyKind(ndt, DateTimeKind.Utc);
                        }
                    }
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
