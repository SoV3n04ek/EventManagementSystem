using EventManagement.Domain.Entities;

namespace EventManagement.Infrastructure.Interfaces
{
    public interface IEventRepository
    {
        Task<IEnumerable<Event>> GetPublicEventsAsync();
        Task<Event?> GetByIdAsync(int id);
        Task AddAsync(Event newEvent);
        void Remove(Event oldEvent);
        Task SaveChangesAsync();
    }
}
