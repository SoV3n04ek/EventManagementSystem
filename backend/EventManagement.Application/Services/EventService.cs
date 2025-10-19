using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Mapping;

namespace EventManagement.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly IUserRepository _userRepository;

        public EventService(
            IEventRepository eventRepository,
            IParticipantRepository participantRepository,
            IUserRepository userRepository)
        {
            _eventRepository = eventRepository;
            _participantRepository = participantRepository;
            _userRepository = userRepository;
        }

        // GET /events
        public async Task<IEnumerable<EventListDto>> GetPublicEventsAsync()
        {
            var events = await _eventRepository.GetPublicEventsAsync();
            return events.Select(e => e.ToListDto());
        }

        // GET /events/{id}
        public async Task<EventDetailDto?> GetEventByIdAsync(int id)
        {
            var ev = await _eventRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Event with ID {id} not found");

            return ev.ToDetailDto();
        }

        // GET /users/{id}/events
        public async Task<IEnumerable<EventListDto>> GetUserEventsAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException($"User with userId:{userId} is not found");

            // organized + joined events
            var joined = user.Participations.Select(p => p.Event).ToList();
            var organized = user.OrganizedEvents;

            return organized.Concat(joined)
                .DistinctBy(e => e.Id)
                .Select(e => e.ToListDto());
        }

        // POST /events
        public async Task<int> CreateEventAsync(CreateEventDto dto, int organizerId)
        {
            var organizer = await _userRepository.GetByIdAsync(organizerId)
                ?? throw new NotFoundException($"Organizer not found");

            var entity = new Event
            {
                Name = dto.Name,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                Capacity = dto.Capacity,
                IsPublic = dto.IsPublic,
                OrganizerId = organizerId
            };

            await _eventRepository.AddAsync(entity);
            await _eventRepository.SaveChangesAsync();

            return entity.Id;
        }

        // PATCH /events/{id}
        public async Task UpdateEventAsync(int eventId, UpdateEventDto dto, int userId)
        {
            Event ev = await _eventRepository.GetByIdAsync(eventId)
                ?? throw new NotFoundException($"Event with eventId {eventId} not found");

            if (ev.OrganizerId != userId)
            {
                throw new ForbiddenException("Only the organizer can edit this event");
            }

            if (dto.Capacity.HasValue && dto.Capacity.Value < ev.ParticipantCount)
            {
                throw new BadRequestException("Capacity cannot be less than current participants");
            }

            ev.Name = dto.Name ?? ev.Name;
            ev.Description = dto.Description ?? ev.Description;
            ev.EventDate = dto.EventDate ?? ev.EventDate;
            ev.Location = dto.Location ?? ev.Location;
            ev.Capacity = dto.Capacity ?? ev.Capacity;
            ev.IsPublic = dto.IsPublic ?? ev.IsPublic;
            ev.UpdatedAt = DateTime.UtcNow;

            await _eventRepository.SaveChangesAsync();
        }

        // DELETE /events/{id}
        public async Task DeleteEventAsync(int eventId, int userId)
        {
            Event ev = await _eventRepository.GetByIdAsync(eventId)
                ?? throw new NotFoundException($"Event with eventId {eventId} not found");

            if (ev.OrganizerId != userId)
                throw new ForbiddenException("You can only delete your own events");

            // TODO: add RemoveById(int id) method in Interface IEventRepository and class EventRepository
            _eventRepository.Remove(ev);
            await _eventRepository.SaveChangesAsync();
        }

        // POST /events/{id}/join
        public async Task JoinEventAsync(int eventId, int userId)
        {
            Event ev = await _eventRepository.GetByIdAsync(eventId)
                ?? throw new NotFoundException($"Event with id {eventId} not found");

            var alreadyJoined = await _participantRepository.GetByEventAndUserAsync(eventId, userId);
            if (alreadyJoined != null)
                throw new Exception("User already joined this event");

            if (ev.IsFull)
                throw new BadRequestException("Event is full");

            var participant = new Participant { EventId = eventId, UserId = userId };
            await _participantRepository.AddAsync(participant);
            await _participantRepository.SaveChangesAsync();
        }

        // POST /events/{id}/leave
        public async Task LeaveEventAsync(int eventId, int userId)
        {
            var participant = await _participantRepository.GetByEventAndUserAsync(eventId, userId)
                ?? throw new NotFoundException($"User with id {userId} not a participant in event with id {eventId}");

            _participantRepository.Remove(participant);
            await _participantRepository.SaveChangesAsync();
        }
    }
}
