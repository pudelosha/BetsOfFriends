namespace Backend.DTOs
{
    public class TournamentMessageDto
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty; // Display name or username
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMessageResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TournamentMessageCreateDto
    {
        public string Content { get; set; }
    }
}
