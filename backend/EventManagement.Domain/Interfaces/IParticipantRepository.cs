using EventManagement.Domain.Entities;

namespace EventManagement.Infrastructure.Interfaces
{
    public interface IParticipantRepository
    {
        Task<Participant?> GetByEventAndUserAsync(int eventId, int userId);
        Task AddAsync(Participant participant);
        void Remove(Participant participant);
        Task SaveChangesAsync();
        Task<int> GetCountByEventIdAsync(int eventId);
    }
}
