using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    /// <summary>
    /// Represents an application user who can create/join tournaments.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public DateTime MemberSince { get; set; } = DateTime.UtcNow;
        public int? LocationId { get; set; }
        public Location? Location { get; set; }
        public int? LanguageId { get; set; }
        public Language? Language { get; set; }
        public string? Nickname { get; set; }

        public bool HintsVisible { get; set; } = true;  // Show page hints/descriptions

        public bool AcceptedRegulations { get; set; }
        public bool AcceptedMarketingConsent { get; set; }

        public bool ReceiveEmailMatchClosed { get; set; }
        public bool ReceivePushMatchClosed { get; set; }

        public bool ReceiveEmailDailyUpdates { get; set; }
        public bool ReceivePushDailyUpdates { get; set; }

        public bool ReceiveEmailTournamentInvitation { get; set; }
        public bool ReceivePushTournamentInvitation { get; set; }

        public bool ReceiveEmailPendingBets { get; set; }
        public bool ReceivePushPendingBets { get; set; }

        public bool ReceiveEmailNewGames { get; set; }
        public bool ReceivePushNewGames { get; set; }

        public bool ReceiveEmailSpecialOffers { get; set; }
        public bool ReceivePushSpecialOffers { get; set; }

        public ICollection<CustomTournamentUserAssignment> CustomTournamentUserAssignments { get; set; } = new List<CustomTournamentUserAssignment>();
    }
}
