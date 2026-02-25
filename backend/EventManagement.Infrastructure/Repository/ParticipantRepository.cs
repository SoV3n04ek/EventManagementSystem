using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Repository;

public class ParticipantRepository(EventManagementDbContext context) : IParticipantRepository
{
    readonly EventManagementDbContext context = context;

    public async Task<Participant?> GetByEventAndUserAsync(int eventId, int userId) => await context.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);

    public async Task<IEnumerable<Participant>> GetByUserIdAsync(int userId) => await context.Participants
            .AsNoTracking()
            .Include(p => p.Event)
            .Where(p => p.UserId == userId)
            .ToListAsync();

    public async Task AddAsync(Participant participant) => _ = await context.Participants.AddAsync(participant);

    public void Remove(Participant participant) => _ = context.Participants.Remove(participant);

    public async Task SaveChangesAsync() => _ = await context.SaveChangesAsync();

    public async Task<int> GetCountByEventIdAsync(int eventId) => await context.Participants
            .Where(p => p.EventId == eventId)
            .CountAsync();
}
