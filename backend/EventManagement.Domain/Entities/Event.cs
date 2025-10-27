using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Domain.Entities
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Location { get; set; } = string.Empty;

        public int? Capacity { get; set; }

        public bool IsPublic { get; set; } = true;

        [Required]
        public int OrganizerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(OrganizerId))]
        public User Organizer { get; set; } = null!;

        public ICollection<Participant> Participants { get; set; } = new List<Participant>();

        // Computed property - Not stored in DB
        [NotMapped]
        public int ParticipantCount => Participants?.Count ?? 0;

        [NotMapped]
        public bool IsFull => Capacity.HasValue && ParticipantCount >= Capacity.Value;

    }
}
