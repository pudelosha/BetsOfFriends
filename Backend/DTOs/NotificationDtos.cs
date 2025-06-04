namespace Backend.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Route { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
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
}
