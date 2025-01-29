using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }
    }
}
