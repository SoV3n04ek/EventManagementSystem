namespace EventManagement.Application.DTOs.EventDtos
{
    public class EventListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
        public bool IsFull { get; set; }
    }
}
