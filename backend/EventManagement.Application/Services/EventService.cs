using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Mapping;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagement.Application.Services;

public class EventService(
    IEventRepository eventRepository,
    IParticipantRepository participantRepository,
    IUserRepository userRepository,
    ILogger<EventService> logger) : IEventService
{
    readonly IEventRepository eventRepository = eventRepository;
    readonly IParticipantRepository participantRepository = participantRepository;
    readonly IUserRepository userRepository = userRepository;
    readonly ILogger<EventService> logger = logger;

    // GET /events
    public async Task<IEnumerable<EventListDto>> GetPublicEventsAsync(int? currentUserId = null)
    {
        var events = await eventRepository.GetPublicEventsAsync();
        return events.Select(e => e.ToListDto(currentUserId));
    }

    // GET /events/{id}
    public async Task<EventDetailDto?> GetEventByIdAsync(int id, int? currentUserId = null)
    {
        var ev = await eventRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Event with ID {id} not found");

        if (!ev.IsPublic)
        {
            logger.LogWarning(
"Access attempt to private event from user with id {UserId}", id);
        }

        return ev.ToDetailDto(currentUserId);
    }

    // GET 

    public async Task<CalendarViewDto> GetUserCalendarAsync(int userId, DateTime startDate, DateTime endDate, string viewType)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException($"User with id {userId} not found");

        // Get organized events
        var organizedEvents = user.OrganizedEvents
            .Where(e => e.EventDate >= startDate && e.EventDate <= endDate)
            .Select(e => new CalendarEventDto
            {
                Id = e.Id,
                Title = e.Name,
                Start = e.EventDate,
                End = e.EventDate.AddHours(2),
                Location = e.Location,
                IsOrganizer = true
            });

        // Get participating events
        var participatingEvents = user.Participations
            .Select(p => p.Event)
            .Where(e => e.EventDate >= startDate && e.EventDate <= endDate)
            .Select(e => new CalendarEventDto
            {
                Id = e.Id,
                Title = e.Name,
                Start = e.EventDate,
                End = e.EventDate.AddHours(2),
                Location = e.Location,
                IsOrganizer = false
            });

        // Combine and remove duplicates by ID, prioritizing the Organizer status
        var allEvents = organizedEvents
            .Concat(participatingEvents)
            .GroupBy(e => e.Id)
            .Select(g => g.OrderByDescending(e => e.IsOrganizer).First())
            .ToList();

        return new CalendarViewDto
        {
            Events = allEvents,
            StartDate = startDate,
            EndDate = endDate,
            ViewType = viewType
        };
    }

    // GET /users/{id}/events
    public async Task<IEnumerable<EventListDto>> GetUserEventsAsync(int userId)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException($"User with userId:{userId} is not found");

        // organized + joined events
        var joined = user.Participations.Select(p => p.Event).ToList();
        var organized = user.OrganizedEvents;

        return organized.Concat(joined)
            .DistinctBy(e => e.Id)
            .Select(e => e.ToListDto(userId));
    }

    // POST /events
    public async Task<int> CreateEventAsync(CreateEventDto dto, int organizerId)
    {
        _ = await userRepository.GetByIdAsync(organizerId)
            ?? throw new NotFoundException($"Organizer not found");

        var entity = new Event
        {
            Name = dto.Name,
            Description = dto.Description,
            EventDate = DateTime.SpecifyKind(dto.EventDate, DateTimeKind.Utc),
            Location = dto.Location,
            Capacity = dto.Capacity,
            IsPublic = dto.IsPublic,
            OrganizerId = organizerId
        };

        await eventRepository.AddAsync(entity);
        await eventRepository.SaveChangesAsync();

        return entity.Id;
    }

    // PATCH /events/{id}
    public async Task UpdateEventAsync(int eventId, UpdateEventDto dto, int userId)
    {
        var ev = await eventRepository.GetByIdAsync(eventId)
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
        ev.EventDate = dto.EventDate.HasValue
            ? DateTime.SpecifyKind(dto.EventDate.Value, DateTimeKind.Utc)
            : ev.EventDate;
        ev.Location = dto.Location ?? ev.Location;
        ev.Capacity = dto.Capacity ?? ev.Capacity;
        ev.IsPublic = dto.IsPublic ?? ev.IsPublic;
        ev.UpdatedAt = DateTime.UtcNow;

        await eventRepository.SaveChangesAsync();
    }

    // DELETE /events/{id}
    public async Task DeleteEventAsync(int eventId, int userId)
    {
        var ev = await eventRepository.GetByIdAsync(eventId)
            ?? throw new NotFoundException($"Event with eventId {eventId} not found");

        if (ev.OrganizerId != userId)
            throw new ForbiddenException("You can only delete your own events");

        eventRepository.Remove(ev);
        await eventRepository.SaveChangesAsync();
    }

    // POST /events/{id}/join
    public async Task JoinEventAsync(int eventId, int userId)
    {
        var ev = await eventRepository.GetByIdAsync(eventId)
            ?? throw new NotFoundException($"Event with id {eventId} not found");

        var alreadyJoined = await participantRepository.GetByEventAndUserAsync(eventId, userId);
        if (alreadyJoined != null)
            throw new ConflictException("User already joined this event");

        int currentParticipants = await participantRepository.GetCountByEventIdAsync(eventId);
        if (ev.Capacity.HasValue && currentParticipants >= ev.Capacity.Value)
        {
            throw new BadRequestException("Event is full");
        }

        var participant = new Participant { EventId = eventId, UserId = userId };
        await participantRepository.AddAsync(participant);
        await participantRepository.SaveChangesAsync();
    }

    // POST /events/{id}/leave
    public async Task LeaveEventAsync(int eventId, int userId)
    {
        var participant = await participantRepository.GetByEventAndUserAsync(eventId, userId)
            ?? throw new NotFoundException($"User with id {userId} not a participant in event with id {eventId}");

        participantRepository.Remove(participant);
        await participantRepository.SaveChangesAsync();
    }
}
