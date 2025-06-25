namespace Backend.Model.Entities
{
    public class PrivateMessage : BaseEntity
    {
        public int Id { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public string RecipientId { get; set; }
        public ApplicationUser Recipient { get; set; }

        public string Subject { get; set; }  // optional
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public bool IsDeletedBySender { get; set; } = false;  // for soft-delete per user
        public bool IsDeletedByRecipient { get; set; } = false;
    }
}
