using EventManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Repository
{
    public class EventRepository : IEventRepository
    {
        private readonly EventManagementDbContext _context;
        public EventRepository(EventManagementDbContext context) =>
            _context = context;

        public async Task<IEnumerable<Event>> GetPublicEventsAsync() =>
            await _context.events
            .Where(e => e.IsPublic)
            .Include(e => e.Participants)
            .ToListAsync();

        public async Task<Event?> GetByIdAsync(int id) =>
            await _context.events
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id);

        public async Task AddAsync(Event newEvent) =>
            await _context.events.AddAsync(newEvent);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
