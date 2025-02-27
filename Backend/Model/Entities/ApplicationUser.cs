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
        public bool AcceptedRegulations { get; set; }
        public bool AcceptedMarketingConsent { get; set; }

        public ICollection<CustomTournamentUserAssignment> CustomTournamentUserAssignments { get; set; } = new List<CustomTournamentUserAssignment>();
    }
}
