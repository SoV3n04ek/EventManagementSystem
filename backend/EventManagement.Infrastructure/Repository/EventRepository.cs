using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Repository;

public class EventRepository(EventManagementDbContext context) : IEventRepository
{
    readonly EventManagementDbContext context = context;

    public async Task<IEnumerable<Event>> GetPublicEventsAsync() =>
        await context.Events
        .Where(e => e.IsPublic)
        .Include(e => e.Participants)
        .ToListAsync();

    public async Task<Event?> GetByIdAsync(int id) =>
        await context.Events
        .Include(e => e.Participants)
        .Include(e => e.Participants)
            .ThenInclude(p => p.User)
        .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<bool> ExistsAsync(int id) => await context.Events.AnyAsync(e => e.Id == id);
    public async Task AddAsync(Event newEvent) =>
        await context.Events.AddAsync(newEvent);

    public void Remove(Event oldEvent) => _ = context.Events.Remove(oldEvent);

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();
}
