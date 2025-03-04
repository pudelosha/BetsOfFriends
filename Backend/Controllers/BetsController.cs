using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;
using Backend.Model.Entities;

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
        [HttpGet("list/{tournamentId}/{status}")]
        public async Task<IActionResult> GetBetsByStatus(int tournamentId, string status)
        {
            try
            {
                _logger.LogInformation($"Fetching bets for tournament {tournamentId} with status {status}");

                // Convert string to BetStatus enum
                if (!Enum.TryParse<Bet.BetStatus>(status, true, out var betStatus))
                {
                    _logger.LogWarning($"Invalid bet status received: {status}");
                    return BadRequest(new { Message = "Invalid bet status." });
                }

                // Get userId from claims
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request to fetch bets.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                // Call service method to get bets
                var bets = await _betService.GetBetsByStatusAsync(tournamentId, userId, betStatus);

                return Ok(bets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching bets for tournament {tournamentId} with status {status}");
                return StatusCode(500, new { Message = "An error occurred while fetching bets." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("calculate/{tournamentId}")]
        public async Task<IActionResult> CalculateBets(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Calculating bets for tournament {tournamentId}");

                var result = await _betService.CalculateBetsAsync(tournamentId);

                if (!result)
                {
                    return BadRequest("Bet calculation failed or no valid bets found.");
                }

                return Ok(new { message = "Bet calculations completed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating bets for tournament {tournamentId}");
                return StatusCode(500, "An error occurred while calculating bets.");
            }
        }
    }
}
