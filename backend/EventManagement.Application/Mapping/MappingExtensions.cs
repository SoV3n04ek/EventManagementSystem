using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.DTOs.ParticipantDtos;
using EventManagement.Application.DTOs.UserDtos;
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
                ShortDescription = ev.Description?.Length > 100 
                    ? ev.Description.Substring(0, 100) + "..." 
                    : ev.Description ?? string.Empty,
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
                ParticipantCount = ev.ParticipantCount,
                Participants = ev.Participants
                .Where(p => p.User != null)
                .Select(p => new ParticipantDto
                {
                    Id = p.User.Id,
                    Name = p.User.Name
                }).ToList()
            };

        public static UserDto ToUserDto(this User user) =>
            new()
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

        public static UserDetailDto ToUserDetailDto(this User user) =>
            new()
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                OrganizedEvents = user.OrganizedEvents.Select(e => e.ToListDto()).ToList(),
                ParticipatingEvents = user.Participations.Select(p => p.Event.ToListDto()).ToList()
            };
    }
}
