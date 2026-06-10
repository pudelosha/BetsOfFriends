using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CustomTournamentDto
    {
        public int? TournamentId { get; set; } // Nullable for new tournaments

        public int? PredefinedTournamentId { get; set; } // Nullable as not mandatory
        public int? Season { get; set; }                // From "filters.season"
        public DateTime? TournamentEnd { get; set; }    // From "matches.season.endDate"

        [Required, MaxLength(100)]
        public string TournamentName { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public bool IncludeHomeAdvantage { get; set; }

        [Required]
        public string TournamentVisibility { get; set; } = "Private";

        [Required]
        public string UpdateMethod { get; set; } = "Manual";

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
        public int? PredefinedTeamId { get; set; }
        [Required, MaxLength(50)]
        public string TeamName { get; set; } = string.Empty;
        public int EloRating { get; set; }
        [Required]
        public string RecordStatus { get; set; } = "New";
    }

    public class CustomStageDto
    {
        public int? StageId { get; set; }
        public int? PredefinedStageId { get; set; }
        public int Order { get; set; }
        [Required, MaxLength(50)]
        public string StageName { get; set; } = string.Empty;
        [Required]
        public string RecordStatus { get; set; } = "New";
    }

    public class CustomMatchDto
    {
        public int? MatchId { get; set; } // Nullable for new matches
        public int? PredefinedMatchId { get; set; } // Nullable as not mandatory
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

        [Required]
        public string RecordStatus { get; set; } = "New";
        public bool IsVisible { get; set; } = true;

        // Properties to handle closed match
        public string? MatchStatus { get; set; }
        public int? ScoreHome { get; set; }
        public int? ScoreAway { get; set; }
        public string? QualifiedTeam { get; set; }
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

        [Required]
        public string RecordStatus { get; set; } = "New";
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
        public string Visibility { get; set; }
        public bool IsVisible { get; set; }
    }

    public class TournamentVisibilityDto
    {
        public bool IsVisible { get; set; }
    }

    public class TournamentSummaryDto
    {
        public bool ShowExactResult { get; set; } = false;
        public bool ShowQualified { get; set; } = false;
        public int MatchesCount { get; set; } = 0;
        public int FinalisedMatchesCount { get; set; } = 0;

        public int Position { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int TotalBetsPlaced { get; set; }
        public decimal BetSuccessRate { get; set; }
        public int Successful1X2Results { get; set; }
        public int SuccessfulQualifications { get; set; }
        public int SuccessfulExactResults { get; set; }
        public decimal TotalPayout { get; set; }
    }

    public class TournamentInvitationRequestDto
    {
        public string Nickname { get; set; }
    }

    public class TournamentJoinRequestDto
    {
        public int TournamentId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
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
        public string Visibility { get; set; }  // "Public" or "Private"
    }

    public class TournamentSearchRequestDto
    {
        public string? SearchTerm { get; set; }
    }

    public class PublicTournamentDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Participants { get; set; }
        public bool JoinRequested { get; set; }
    }

    public class TournamentParticipantDto
    {
        public int AssignmentId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ParticipantActionRequest
    {
        public string UserEmail { get; set; }
    }

    public class TournamentAssignmentDto
    {
        public string Nickname { get; set; } = string.Empty;
    }

    public class UpdateAssignmentRequestDto
    {
        public string Nickname { get; set; }
    }

    public class SelectedTournamentDetailsDto
    {
        public string TournamentName { get; set; } = string.Empty;
        public int MatchesCount { get; set; }
        public int FinalisedMatchesCount { get; set; }
    }

    public class TournamentCreationResultDto
    {
        public bool Success { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string CreatorUserId { get; set; }
        public List<string> InvitedEmails { get; set; } = new();
        public List<string> ExistingEmails { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public static TournamentCreationResultDto SuccessResult(int tournamentId, string tournamentName, string creatorUserId, IEnumerable<string> invited, IEnumerable<string> existing)
        {
            return new TournamentCreationResultDto
            {
                Success = true,
                TournamentId = tournamentId,
                TournamentName = tournamentName,
                CreatorUserId = creatorUserId,
                InvitedEmails = invited.ToList(),
                ExistingEmails = existing.ToList()
            };
        }

        public static TournamentCreationResultDto ErrorResult(string errorMessage)
        {
            return new TournamentCreationResultDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class TournamentUpdateResultDto
    {
        public bool Success { get; set; }
        public int TournamentId { get; set; }
        public string? TournamentName { get; set; }
        public HashSet<string> InvitedEmails { get; set; } = new();
        public HashSet<string> ExistingEmails { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public static TournamentUpdateResultDto SuccessResult(int tournamentId, string? name, HashSet<string> invited, HashSet<string> existing)
        {
            return new TournamentUpdateResultDto
            {
                Success = true,
                TournamentId = tournamentId,
                TournamentName = name,
                InvitedEmails = invited,
                ExistingEmails = existing
            };
        }

        public static TournamentUpdateResultDto ErrorResult(string message) =>
            new TournamentUpdateResultDto { Success = false, ErrorMessage = message };
    }

    public class MatchInsightDto
    {
        public int MatchId { get; set; }
        public string Stage { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string? Result { get; set; }
        public string MatchDateTime { get; set; }
        public string MatchStatus { get; set; } // 'Upcoming' | 'InProgress' | 'Finalized'
        public bool ShowExactResult { get; set; }
        public bool ShowQualified { get; set; }
        public List<MatchUserBetDto> UserBets { get; set; } = new();
    }

    public class MatchUserBetDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public string BetScore { get; set; } = string.Empty;
        public int ResultSuccess { get; set; } // 0 or 1
        public int? PreciseResultSuccess { get; set; } // optional
        public int? QualificationSuccess { get; set; } // optional
        public decimal TotalPayout { get; set; }
    }

    public class CustomTournamentExtraPredictionsDto
    {
        public int TournamentId { get; set; }
        public bool IsLocked { get; set; }
        public List<CustomTournamentExtraPredictionTeamDto> Teams { get; set; } = new();
        public List<CustomTournamentExtraPredictionRowDto> Predictions { get; set; } = new();
    }

    public class CustomTournamentExtraPredictionTeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
    }

    public class CustomTournamentExtraPredictionRowDto
    {
        public int TournamentId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public bool IsCurrentUser { get; set; }
        public bool HasPrediction { get; set; }
        public int? WinnerTeamId { get; set; }
        public int? SecondPlaceTeamId { get; set; }
        public int? ThirdPlaceTeamId { get; set; }
        public int? TopScorerTeamId { get; set; }
        public string TopScorerName { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CustomTournamentExtraPredictionUpdateDto
    {
        public int? WinnerTeamId { get; set; }
        public int? SecondPlaceTeamId { get; set; }
        public int? ThirdPlaceTeamId { get; set; }
        public int? TopScorerTeamId { get; set; }

        [MaxLength(80)]
        public string? TopScorerName { get; set; }
    }
}
