using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("tournament-selection")]
    [ApiController]
    public class TournamentSelectionController : ControllerBase
    {
        private readonly ITournamentSelectionService _tournamentSelectionService;
        private readonly IUserService _userService;
        private readonly ILogger<TournamentSelectionController> _logger;

        public TournamentSelectionController(
            ITournamentSelectionService tournamentSelectionService,
            IUserService userService,
            ILogger<TournamentSelectionController> logger)
        {
            _tournamentSelectionService = tournamentSelectionService;
            _userService = userService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("set/{tournamentId}")]
        public async Task<IActionResult> SetSelectedTournament(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var success = await _tournamentSelectionService.SetSelectedTournamentAsync(userId, tournamentId);

                if (!success)
                {
                    return BadRequest(new { Message = "Invalid tournament selection or assignment not found." });
                }

                return Ok(new { Message = "Tournament selection updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting selected tournament.");
                return StatusCode(500, new { Message = "An error occurred while setting the selected tournament." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("get")]
        public async Task<IActionResult> GetSelectedTournament()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var tournamentId = await _tournamentSelectionService.GetSelectedTournamentAsync(userId);

                return Ok(new { tournamentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching selected tournament.");
                return StatusCode(500, new { Message = "An error occurred while fetching the selected tournament." });
            }
        }
    }
}
