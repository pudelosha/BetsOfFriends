using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Backend.Model.Entities;
using Backend.Repository.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/bets")]
    public class BetsController : ControllerBase
    {
        private readonly IBetService _betService;
        private readonly IUserService _userService;
        private readonly ILogger<BetsController> _logger;

        public BetsController(IBetService betService, IUserService userService, ILogger<BetsController> logger)
        {
            _betService = betService;
            _userService = userService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPut("update/{betId}")]
        public async Task<IActionResult> UpdateBet(int betId, [FromBody] BetUpdateDto betUpdateDto)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized("User not found.");
                }

                _logger.LogInformation($"User {userId} is attempting to update bet {betId}");

                var result = await _betService.UpdateBetAsync(betId, userId, betUpdateDto);

                if (!result)
                {
                    return NotFound($"Bet with ID {betId} not found or update failed.");
                }

                return Ok(new { message = "Bet updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating bet with ID {betId}");
                return StatusCode(500, "An error occurred while updating the bet.");
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("{tournamentId}/{status}/{stage}")]
        public async Task<IActionResult> GetTournamentBets(int tournamentId, string status, string stage)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var matches = await _betService.GetBetsByStatusAndStageAsync(tournamentId, userId, status, stage);

                return Ok(matches ?? Enumerable.Empty<BetDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching bets for tournament {tournamentId}, status {status}, stage {stage}.");
                return StatusCode(500, new { Message = "An error occurred while fetching bets." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("stats/{matchId}")]
        public async Task<IActionResult> GetBetStatsByMatchId(int matchId)
        {
            try
            {
                _logger.LogInformation($"Received request for bet statistics for match ID: {matchId}");

                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized("User not found.");
                }

                var betStats = await _betService.GetBetStatisticsAsync(matchId, userId);

                if (betStats == null)
                {
                    _logger.LogWarning($"No betting statistics found for match ID {matchId}");
                    return NotFound(new { message = "No betting statistics found for this match." });
                }

                return Ok(betStats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error while fetching bet stats for match ID {matchId}: {ex.Message}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("upcoming/{tournamentId}")]
        public async Task<IActionResult> GetUpcomingBets(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Fetching upcoming bets for tournament {tournamentId}");

                // Get userId from claims
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request to fetch upcoming bets.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                // Call service method to get upcoming bets
                var upcomingBets = await _betService.GetUpcomingBetsAsync(tournamentId, userId, 5);

                return Ok(upcomingBets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching upcoming bets for tournament {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while fetching upcoming bets." });
            }
        }
    }
}
