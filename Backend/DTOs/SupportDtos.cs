namespace Backend.DTOs
{
    public class SupportMessageDto
    {
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Language { get; set; } = "en";
    }
}
