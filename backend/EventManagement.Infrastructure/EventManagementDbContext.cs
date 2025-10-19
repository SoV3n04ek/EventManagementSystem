using EventManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure
{
    public class EventManagementDbContext : DbContext
    {
        public EventManagementDbContext(DbContextOptions options) 
            : base(options) { }
        

        public DbSet<User> Users => Set<User>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Participant> Participants => Set<Participant>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Force all table names to lowercase
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName()!.ToLower());
            }

            // User -> Event (Organizer)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Event <-> Participant <-> User (many-to-many)
            modelBuilder.Entity<Participant>()
                .HasOne(p => p.Event)
                .WithMany(e => e.Participants)
                .HasForeignKey(p => p.EventId);

            modelBuilder.Entity<Participant>()
                .HasOne(p => p.User)
                .WithMany(u => u.Participations)
                .HasForeignKey(p => p.UserId);

            // Optional: unique constraint to prevent duplicate participation
            modelBuilder.Entity<Participant>()
                .HasIndex(p => new { p.EventId, p.UserId })
                .IsUnique();
        }
    }
}
