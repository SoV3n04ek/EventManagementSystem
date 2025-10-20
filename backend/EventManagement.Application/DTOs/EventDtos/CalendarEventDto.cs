
namespace EventManagement.Application.DTOs.EventDtos
{
    public class CalendarEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool IsOrganizer { get; set; }
    }
}
