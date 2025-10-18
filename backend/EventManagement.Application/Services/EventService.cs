using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly EventManagementDbContext _context;

        public EventService(IEventRepository eventRepository, EventManagementDbContext context)
        {
            _eventRepository = eventRepository;
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetPublicEventsAsync()
        {
            return await _eventRepository.GetPublicEventsAsync();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _eventRepository.GetByIdAsync(id);
        }

        public async Task JoinEventAsync(int eventId, int userId)
        {
            var eventEntity = await _eventRepository.GetByIdAsync(eventId)
                              ?? throw new Exception("Event not found");

            // Check if already joined
            bool alreadyJoined = await _context.Participants
                .AnyAsync(p => p.EventId == eventId && p.UserId == userId);
            if (alreadyJoined)
                throw new Exception("User already joined this event");

            // Check if full
            if (eventEntity.Capacity.HasValue && eventEntity.Participants.Count >= eventEntity.Capacity.Value)
                throw new Exception("Event is full");

            var participant = new Participant
            {
                EventId = eventId,
                UserId = userId
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
        }

        public async Task LeaveEventAsync(int eventId, int userId)
        {
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);

            if (participant == null)
                throw new Exception("User is not a participant in this event");

            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();
        }

        public Task<IEnumerable<EventListDto>> GetEventsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<EventListDto>> GetUserEventsAsync(int userId)
        {
            throw new NotImplementedException();
        }
    }
}
