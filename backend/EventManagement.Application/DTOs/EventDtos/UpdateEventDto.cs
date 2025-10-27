namespace EventManagement.Application.DTOs.EventDtos
{
    public class UpdateEventDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? EventDate { get; set; }
        public string? Location { get; set; }
        public int? Capacity { get; set; }
        public bool? IsPublic { get; set; }
    }
}
