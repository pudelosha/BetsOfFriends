namespace Backend.Model.Entities
{
    public class NotificationRecipient : BaseEntity
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int NotificationId { get; set; }
        public Notification Notification { get; set; }

        public bool IsRead { get; set; } = false;
        public bool SentEmail { get; set; } = false;
        public bool SentPush { get; set; } = false;
    }
}
