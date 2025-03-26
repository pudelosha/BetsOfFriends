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
        [HttpGet("{tournamentId}/{status}/{stage}")]
        public async Task<IActionResult> GetTournamentMatches(int tournamentId, string status, string stage)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var matches = await _matchService.GetMatchesByStatusAndStageAsync(tournamentId, userId, status, stage);

                if (matches == null || !matches.Any())
                {
                    return NotFound(new { Message = "No matches found for the given criteria." });
                }

                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching matches for tournament {tournamentId}, status {status}, stage {stage}.");
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

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("started/{tournamentId}")]
        public async Task<IActionResult> GetStartedCustomMatches(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var matches = await _matchService.GetStartedMatchesAsync(tournamentId, userId);

                if (matches == null || !matches.Any())
                {
                    return NotFound(new { Message = "No started custom matches found." });
                }

                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching started custom matches for tournament {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while fetching matches." });
            }
        }
    }
}
