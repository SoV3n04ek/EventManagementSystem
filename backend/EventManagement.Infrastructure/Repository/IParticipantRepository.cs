using EventManagement.Domain.Entities;

namespace EventManagement.Infrastructure.Repository
{
    public interface IParticipantRepository
    {
        Task<Participant?> GetByEventAndUserAsync(int eventId, int userId);
        Task AddAsync(Participant participant);
        Task RemoveAsync(Participant participant);
        Task SaveChangesAsync();
    }
}
