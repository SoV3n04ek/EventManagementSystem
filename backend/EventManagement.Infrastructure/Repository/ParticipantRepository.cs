using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Repository
{
    public class ParticipantRepository : IParticipantRepository
    {
        private readonly EventManagementDbContext _context;

        public ParticipantRepository(EventManagementDbContext context)
        {
            _context = context;
        }

        public async Task<Participant?> GetByEventAndUserAsync(int eventId, int userId)
        {
            return await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);
        }

        public async Task<IEnumerable<Participant>> GetByUserIdAsync(int userId)
        {
            return await _context.Participants
                .AsNoTracking()
                .Include(p => p.Event)
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(Participant participant)
        {
            await _context.Participants.AddAsync(participant);
        }

        public void Remove(Participant participant)
        {
            _context.Participants.Remove(participant);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
