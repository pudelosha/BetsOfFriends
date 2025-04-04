namespace Backend.Model.Entities
{
    public class Notification : BaseEntity
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Route { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
