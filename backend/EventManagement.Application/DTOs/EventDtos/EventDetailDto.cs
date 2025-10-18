using EventManagement.Application.DTOs.ParticipantDtos;

namespace EventManagement.Application.DTOs.EventDtos
{
    public class EventDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public bool IsPublic { get; set; }
        public string OrganizerName { get; set; } = string.Empty;

        public List<ParticipantDto> Participants { get; set; } = new();
    }
}
