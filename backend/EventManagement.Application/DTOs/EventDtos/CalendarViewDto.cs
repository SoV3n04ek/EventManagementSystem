namespace EventManagement.Application.DTOs.EventDtos
{
    public class CalendarViewDto
    {
        public List<CalendarEventDto> Events { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ViewType { get; set; } = "month";
    }
}
