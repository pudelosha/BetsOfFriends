using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class PredefinedTournamentDto
    {
        public int? TournamentId { get; set; } // Nullable for new tournaments

        [Required, MaxLength(100)]
        public string TournamentName { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public List<PredefinedTeamDto> Teams { get; set; } = new();
        [Required]
        public List<PredefinedMatchDto> Matches { get; set; } = new();
        [Required]
        public List<PredefinedStageDto> Stages { get; set; } = new();
    }

    public class PredefinedTeamDto
    {
        public int? TeamId { get; set; } // Nullable for new teams

        [Required, MaxLength(50)]
        public string TeamName { get; set; } = string.Empty;
        [Required]
        public string RecordStatus { get; set; }
    }

    public class PredefinedStageDto
    {
        public int? StageId { get; set; }
        public int Order { get; set; }

        [Required, MaxLength(50)]
        public string StageName { get; set; } = string.Empty;
        [Required]
        public string RecordStatus { get; set; }
    }

    public class PredefinedMatchDto
    {
        public int? MatchId { get; set; } // Nullable for new matches

        public int? StageId { get; set; } // Nullable for new matches
        [Required]
        public string StageName { get; set; }

        public int? HomeTeamId { get; set; } // Nullable for new matches
        [Required]
        public string HomeTeam { get; set; } = string.Empty;

        public int? AwayTeamId { get; set; } // Nullable for new matches
        [Required]
        public string AwayTeam { get; set; } = string.Empty;

        [Required]
        public string MatchType { get; set; } = "Regular90Min"; // Default value if missing

        public decimal HomeWinOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayWinOdds { get; set; }
        public decimal? HomeQualifies { get; set; }
        public decimal? AwayQualifies { get; set; }

        [Required]
        public string RecordStatus { get; set; }

        private DateTime _matchStart;

        [Required]
        public DateTime MatchStart
        {
            get => _matchStart;
            set => _matchStart = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }

    public class PredefinedTournamentListDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class TournamentStatusUpdateDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
