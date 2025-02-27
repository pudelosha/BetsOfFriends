using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model.Entities
{
    public class Bet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BetId { get; set; }

        public int MatchId { get; set; }
        public CustomMatch Match { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public decimal BaseAmount { get; set; } = 1;
        public decimal? BonusAmount { get; set; }

        public int? HomeGoals { get; set; }
        public int? AwayGoals { get; set; }
        public Team? QualifiedTeam { get; set; }

        public BetResult Result { get; set; }
        public bool Submitted { get; set; } = false;
        public decimal? Payout { get; set; }

        public enum BetResult
        {
            Won,
            Lost,
            Pending
        }

        public enum Team
        {
            Home,
            Away
        }
    }
}
