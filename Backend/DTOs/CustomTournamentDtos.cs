using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CustomTournamentDto
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
        public List<CustomTeamDto> Teams { get; set; } = new();

        [Required]
        public List<CustomMatchDto> Matches { get; set; } = new();

        [Required]
        public List<CustomUserDto> Users { get; set; } = new();
        [Required]
        public List<CustomStageDto> Stages { get; set; } = new();

        public CustomTournamentSettingsDto? Settings { get; set; }
    }

    public class CustomTournamentSettingsDto
    {
        [Required]
        public bool AllowExactResultBonus { get; set; } = false;

        [Required]
        public string ExactResultBonusCalculation { get; set; } = "FixedValue"; // Stored as string

        [Range(1, int.MaxValue, ErrorMessage = "Bonus value must be at least 1.")]
        public int? ExactResultBonus { get; set; }

        [Required]
        public bool AllowWhoQualifiesBets { get; set; } = false;

        [Required]
        public bool AllowBetsWithBooster { get; set; } = false;

        [Range(1, int.MaxValue, ErrorMessage = "Max bet booster must be at least 1.")]
        public int MaxBetBooster { get; set; } = 1;

        [Range(1, int.MaxValue, ErrorMessage = "Total booster pool must be at least 1.")]
        public int? TotalBoosterPool { get; set; }

        [Required]
        public bool AllowNonSubmittedBetsPenalty { get; set; } = false;

        [Range(0, int.MaxValue, ErrorMessage = "Penalty must be non-negative.")]
        public int? NonSubmittedBetPenalty { get; set; }

        [Required]
        public string TournamentVisibility { get; set; } = "Private";
        [MaxLength(100)]
        public string? PublicTournamentName { get; set; }
        [Required]
        public string UpdateMethod { get; set; } = "Manual";
    }

    public class CustomTeamDto
    {
        public int? TeamId { get; set; }

        [Required, MaxLength(50)]
        public string TeamName { get; set; } = string.Empty;
    }

    public class CustomStageDto
    {
        public int? StageId { get; set; }
        public int Order { get; set; }

        [Required, MaxLength(50)]
        public string StageName { get; set; } = string.Empty;
    }

    public class CustomMatchDto
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

        private DateTime _matchStart;

        [Required]
        public DateTime MatchStart
        {
            get => _matchStart;
            set => _matchStart = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }

    public class CustomUserDto
    {
        public int? AssignmentId { get; set; } // Nullable for new users

        [MaxLength(100)]
        public string? UserName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string UserAdminName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "New"; // Status can be 'New', 'Invited', 'Accepted', or 'Banned'
        public string UserRole { get; set; } = "Player"; // Role can be 'Player', 'Admin'
    }

    public class CustomTournamentListDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomTournamentStatusUpdateDto
    {
        [Required]
        public bool IsActive { get; set; }
    }

    public class UserActiveTournamentDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public int AssignmentId { get; set; }
        public string UserName { get; set; }
        public int NumberOfParticipants { get; set; }
        public string Role { get; set; }
        public string AssignmentStatus { get; set; }
        public bool IsVisible { get; set; }
    }

    public class TournamentVisibilityDto
    {
        public bool IsVisible { get; set; }
    }

    public class TournamentSummaryDto
    {
        public int Position { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int TotalBetsPlaced { get; set; }
        public int Successful1X2Results { get; set; }
        public int SuccessfulQualifications { get; set; }
        public int SuccessfulExactResults { get; set; }
        public decimal TotalPayout { get; set; }
    }

    public class TournamentInvitationRequestDto
    {
        public string Nickname { get; set; }
    }

    public class TournamentInvitationResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class TournamentPlayerResultDto
    {
        public int Position { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class TournamentInviteDto
    {
        public string TournamentName { get; set; }
        public int NumberOfParticipants { get; set; }
        public string AssignmentStatus { get; set; }
    }

    public class CustomTournamentNameDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
