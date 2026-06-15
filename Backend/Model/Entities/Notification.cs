namespace Backend.Model.Entities
{
    public class Notification : BaseEntity
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Route { get; set; }
        public string? Type { get; set; }
        public string? SenderUserId { get; set; }
        public string? SenderDisplayName { get; set; }
        public int? TournamentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
