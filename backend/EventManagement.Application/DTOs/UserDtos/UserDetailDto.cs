using EventManagement.Application.DTOs.EventDtos;

namespace EventManagement.Application.DTOs.UserDtos
{
    public class UserDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // User's events and participations
        public List<EventListDto> OrganizedEvents { get; set; } = new();
        public List<EventListDto> ParticipatingEvents { get; set; } = new();
    }
}
