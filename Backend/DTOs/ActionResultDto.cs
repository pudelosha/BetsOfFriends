namespace Backend.DTOs
{
    public class ActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static ActionResultDto SuccessResult(string message) => new ActionResultDto { Success = true, Message = message };
        public static ActionResultDto ErrorResult(string message) => new ActionResultDto { Success = false, Message = message };
    }
}
