using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    /// <summary>
    /// Represents a football tournament where users can bet on matches.
    /// </summary>
    public class Tournament
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserTournament> UserTournaments { get; set; } = new List<UserTournament>();
    }
}
