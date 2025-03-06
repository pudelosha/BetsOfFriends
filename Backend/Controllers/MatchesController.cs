using Backend.DTOs.Backend.DTOs;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Backend.Model.Entities.CustomMatch;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly IUserService _userService;
        private readonly ILogger<MatchesController> _logger;

        public MatchesController(IMatchService matchService, IUserService userService, ILogger<MatchesController> logger)
        {
            _matchService = matchService;
            _userService = userService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("list/{tournamentId}/{status}")]
        public async Task<IActionResult> GetMatchesByStatus(int tournamentId, string status)
        {
            try
            {
                _logger.LogInformation($"Fetching matches for tournament {tournamentId} with status {status}");

                // Validate match status
                if (!Enum.TryParse<MatchStatus>(status, true, out var matchStatus))
                {
                    _logger.LogWarning($"Invalid match status received: {status}");
                    return BadRequest(new { Message = "Invalid match status." });
                }

                // Fetch user from claims
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request to fetch matches.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                // Call service with user ID
                var matches = await _matchService.GetMatchesByStatusAsync(tournamentId, userId, matchStatus);
                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching matches for tournament {tournamentId} with status {status}");
                return StatusCode(500, new { Message = "An error occurred while fetching matches." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
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

                var updated = await _matchService.UpdateMatchResultAsync(matchUpdateDto, userId);

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
    }


}
