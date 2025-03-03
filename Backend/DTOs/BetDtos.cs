using Backend.Model.Entities;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class BetUpdateDto
    {
        [Required]
        public decimal BaseAmount { get; set; }

        public decimal? BonusAmount { get; set; }

        [Range(0, 20, ErrorMessage = "Goals must be between 0 and 20.")]
        public int? HomeGoals { get; set; }

        [Range(0, 20, ErrorMessage = "Goals must be between 0 and 20.")]
        public int? AwayGoals { get; set; }

        public Bet.Team? QualifiedTeam { get; set; }
    }

    public class BetDto
    {
        public int BetId { get; set; }
        public int MatchId { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal? BonusAmount { get; set; }
        public int? HomeGoals { get; set; }
        public int? AwayGoals { get; set; }
        public Bet.Team? QualifiedTeam { get; set; }
        public bool Submitted { get; set; }
        public decimal? Payout { get; set; }
        public string Result { get; set; } // Enum stored as a string (Won, Lost, Pending)
    }
}
