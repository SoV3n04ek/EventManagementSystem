using EventManagement.Application.DTOs.EventDtos;

namespace EventManagement.Application.Interfaces
{
    public interface IEventService
    {
        Task<IEnumerable<EventListDto>> GetEventsAsync();
        //Task<EventDetailDto?> GetEventByIdAsync(int id);
        Task<IEnumerable<EventListDto>> GetUserEventsAsync(int userId);
        Task JoinEventAsync(int eventId, int userId);
        Task LeaveEventAsync(int eventId, int userId);

    }
}
