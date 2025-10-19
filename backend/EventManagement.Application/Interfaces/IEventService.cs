using EventManagement.Application.DTOs.EventDtos;

namespace EventManagement.Application.Interfaces
{
    public interface IEventService
    {
        Task<IEnumerable<EventListDto>> GetPublicEventsAsync();
        Task<EventDetailDto?> GetEventByIdAsync(int id);
        Task<IEnumerable<EventListDto>> GetUserEventsAsync(int userId);
        Task<int> CreateEventAsync(CreateEventDto dto, int organizerId);
        Task UpdateEventAsync(int eventId, UpdateEventDto dto, int userId);
        Task DeleteEventAsync(int eventId, int userId);
        Task JoinEventAsync(int eventId, int userId);
        Task LeaveEventAsync(int eventId, int userId);
    }
}
