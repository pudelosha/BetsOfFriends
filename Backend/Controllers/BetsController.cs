using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;

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
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized("User not found.");
                }

                _logger.LogInformation($"Fetching bets for user {userId}, tournament {tournamentId}, status {status}");

                var bets = await _betService.GetBetsByStatusAsync(tournamentId, userId, status);

                if (bets == null || !bets.Any())
                {
                    return NotFound($"No bets found for tournament {tournamentId} with status {status}.");
                }

                return Ok(bets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching bets for tournament {tournamentId} with status {status}");
                return StatusCode(500, "An error occurred while retrieving bets.");
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
