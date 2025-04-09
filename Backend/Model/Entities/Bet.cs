using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Backend.Model.Entities.CustomMatch;

namespace Backend.Model.Entities
{
    public class Bet : BaseEntity
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
        public TeamQualified? Qualified { get; set; }


        public BetStatus Status { get; set; }
        public BetResult Result { get; set; }
        public bool Calculated { get; set; } = false;

        public decimal? BasePayout { get; set; }            // 1x2 outcome
        public decimal? QualificationPayout { get; set; }   // Correct qualifier
        public decimal? ExactScorePayout { get; set; }      // Exact result

        public enum BetStatus
        {
            ToPlace,
            Placed,
            Closed
        }

        public enum BetResult
        {
            Pending,
            Won,
            Lost        
        }
    }
}
