namespace Backend.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Route { get; set; }
        public string? Type { get; set; }
        public string? SenderUserId { get; set; }
        public string? SenderDisplayName { get; set; }
        public int? TournamentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class NotificationMessageRecipientDto
    {
        public int AssignmentId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class SendTournamentUserMessageDto
    {
        public int TournamentId { get; set; }
        public int RecipientAssignmentId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ReplyToUserMessageDto
    {
        public string RecipientUserId { get; set; } = string.Empty;
        public int? TournamentId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SendAdminBroadcastMessageDto
    {
        public string Message { get; set; } = string.Empty;
        public bool SendEmail { get; set; }
    }

    public class SendAdminUserMessageDto
    {
        public string RecipientUserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool SendEmail { get; set; }
    }

    public class NotificationSettingsDto
    {
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
    }

    public class PushPublicKeyDto
    {
        public bool Enabled { get; set; }
        public string? PublicKey { get; set; }
    }

    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public long? ExpirationTime { get; set; }
        public PushSubscriptionKeysDto Keys { get; set; } = new();
        public string? UserAgent { get; set; }
    }

    public class PushSubscriptionKeysDto
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class PushSubscriptionDeleteDto
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}
