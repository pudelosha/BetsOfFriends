namespace Backend.Model.Entities
{
    /// <summary>
    /// Many-to-Many Relationship Table Between Users and Tournaments.
    /// </summary>
    public class UserTournament
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public UserTournamentRole Role { get; set; }
        public bool IsConfirmed { get; set; }
    }

    public enum UserTournamentRole
    {
        Guest,
        Admin
    }
}
