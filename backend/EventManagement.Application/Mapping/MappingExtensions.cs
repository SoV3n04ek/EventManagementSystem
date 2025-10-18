using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.DTOs.ParticipantDtos;
using EventManagement.Domain.Entities;

namespace EventManagement.Application.Mapping
{
    public static class MappingExtensions
    {
        public static EventListDto ToListDto(this Event ev) =>
            new()
            {
                Id = ev.Id,
                Name = ev.Name,
                EventDate = ev.EventDate,
                Location = ev.Location,
                ParticipantCount = ev.ParticipantCount,
                IsFull = ev.IsFull
            };

        public static EventDetailDto ToDetailDto(this Event ev) =>
            new()
            {
                Id = ev.Id,
                Name = ev.Name,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Capacity = ev.Capacity,
                IsPublic = ev.IsPublic,
                OrganizerName = ev.Organizer?.Name ?? "Unknown",
                Participants = ev.Participants.Select(p => new ParticipantDto
                {
                    Id = p.User.Id,
                    Name = p.User.Name
                }).ToList()
            };
    }
}
