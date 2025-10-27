namespace EventManagement.Application.DTOs.ParticipantDtos
{
    public class ParticipantDetailDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
