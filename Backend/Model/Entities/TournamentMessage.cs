namespace Backend.Model.Entities
{
    public class TournamentMessage : BaseEntity
    {
        public int Id { get; set; }

        public int TournamentId { get; set; }
        public CustomTournament Tournament { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
