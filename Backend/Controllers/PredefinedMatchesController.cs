using Backend.DTOs.Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/predefined-matches")]
    [ApiController]
    public class PredefinedMatchesController : ControllerBase
    {
        private readonly IPredefinedMatchService _matchService;
        private readonly IUserService _userService;
        private readonly ILogger<PredefinedMatchesController> _logger;

        public PredefinedMatchesController(IPredefinedMatchService matchService, IUserService userService, ILogger<PredefinedMatchesController> logger)
        {
            _matchService = matchService;
            _userService = userService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("{tournamentId}/{status}/{stage}")]
        public async Task<IActionResult> GetTournamentMatches(int tournamentId, string status, string stage)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request - user not found.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var matches = await _matchService.GetMatchesByStatusAndStageAsync(tournamentId, status, stage);

                return Ok(matches ?? Enumerable.Empty<MatchDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching matches for tournament {tournamentId}, status {status}, stage {stage}.");
                return StatusCode(500, new { Message = "An error occurred while fetching matches." });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPatch("update/{matchId}")]
        public async Task<IActionResult> UpdateMatchResult(int matchId, [FromBody] MatchResultUpdateDto matchUpdateDto)
        {
            try
            {
                if (matchId != matchUpdateDto.MatchId)
                {
                    _logger.LogWarning($"Mismatched Match ID: URL {matchId} vs Body {matchUpdateDto.MatchId}");
                    return BadRequest(new { Message = "Match ID mismatch." });
                }

                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request - user not found.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var updated = await _matchService.UpdateMatchResultAsync(matchUpdateDto);

                if (!updated)
                {
                    return NotFound(new { Message = $"Match ID {matchId} not found or no updates were made." });
                }

                return Ok(new { Message = "Match updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating match ID {matchId}");
                return StatusCode(500, new { Message = "An error occurred while updating the match." });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("started")]
        public async Task<IActionResult> GetStartedPredefinedMatches()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var matches = await _matchService.GetStartedMatchesAsync();

                return Ok(matches ?? Enumerable.Empty<MatchDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching started predefined matches.");
                return StatusCode(500, new { Message = "An error occurred while fetching matches." });
            }
        }
    }
}
