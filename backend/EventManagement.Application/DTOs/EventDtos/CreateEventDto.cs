namespace EventManagement.Application.DTOs.EventDtos
{
    public class CreateEventDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public bool IsPublic { get; set; }
    }
}
