using Backend.DTOs;
using Backend.Repository.Interfaces;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("tournament-messages")]
[ApiController]
public class TournamentMessagesController : ControllerBase
{
    private readonly ITournamentMessageService _messageService;
    private readonly IUserService _userService;
    private readonly ILogger<TournamentMessagesController> _logger;

    public TournamentMessagesController(
        ITournamentMessageService messageService,
        IUserService userService,
        ILogger<TournamentMessagesController> logger)
    {
        _messageService = messageService;
        _userService = userService;
        _logger = logger;
    }

    [Authorize(Roles = "SuperAdmin,Admin,User")]
    [HttpGet("{tournamentId}")]
    public async Task<IActionResult> GetMessages(int tournamentId)
    {
        try
        {
            var userId = _userService.GetUserIdFromClaims(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User authentication failed." });

            var messages = await _messageService.GetLatestMessagesAsync(tournamentId, userId, 10);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tournament messages.");
            return StatusCode(500, new { Message = "An error occurred while fetching messages." });
        }
    }

    [Authorize(Roles = "SuperAdmin,Admin,User")]
    [HttpPost("{tournamentId}")]
    public async Task<IActionResult> PostMessage(int tournamentId, [FromBody] TournamentMessageCreateDto request)
    {
        try
        {
            var userId = _userService.GetUserIdFromClaims(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User authentication failed." });

            var result = await _messageService.CreateMessageAsync(tournamentId, userId, request.Content);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error posting message for tournament {tournamentId}.");
            return StatusCode(500, new { Message = "An error occurred while posting message." });
        }
    }
}
