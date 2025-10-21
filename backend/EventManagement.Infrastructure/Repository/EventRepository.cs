using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Repository
{
    public class EventRepository : IEventRepository
    {
        private readonly EventManagementDbContext _context;
        public EventRepository(EventManagementDbContext context) =>
            _context = context;

        public async Task<IEnumerable<Event>> GetPublicEventsAsync() =>
            await _context.Events
            .Where(e => e.IsPublic)
            .Include(e => e.Participants)
            .ToListAsync();

        public async Task<Event?> GetByIdAsync(int id) =>
            await _context.Events
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id);

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Events.AnyAsync(e => e.Id == id);
        }

        public async Task AddAsync(Event newEvent) =>
            await _context.Events.AddAsync(newEvent);

        public void Remove(Event oldEvent)
        {
            _context.Events.Remove(oldEvent);
        }

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
