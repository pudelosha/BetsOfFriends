using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/custom-tournaments")]
    public class CustomTournamentsController : ControllerBase
    {
        private readonly ICustomTournamentService _tournamentService;
        private readonly ILogger<CustomTournamentsController> _logger;
        private readonly IUserService _userService;

        public CustomTournamentsController(ICustomTournamentService tournamentService, ILogger<CustomTournamentsController> logger, IUserService userService)
        {
            _tournamentService = tournamentService;
            _logger = logger;
            _userService = userService;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomTournament([FromBody] CustomTournamentDto tournamentDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid tournament data received.");
                return BadRequest(ModelState);
            }

            // Get Logged-in User ID from Claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized tournament creation attempt.");
                return Unauthorized("User is not authenticated.");
            }

            // Fetch User from DB (Ensure User Exists)
            var user = await _userService.FindUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User ID {userId} not found.");
                return Unauthorized("Invalid user.");
            }

            _logger.LogInformation($"User {user.Email} ({userId}) is creating a tournament.");

            // Set CreatedBy field with logged-in user ID
            tournamentDto.CreatedBy = userId;

            // Call Tournament Service
            bool success = await _tournamentService.CreateCustomTournamentAsync(tournamentDto);

            if (!success)
            {
                return StatusCode(500, "Error inserting tournament.");
            }

            return Ok(new { message = "Tournament created successfully!" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllCustomTournaments()
        {
            try
            {
                var tournaments = await _tournamentService.GetAllCustomTournamentsAsync();
                return Ok(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching tournaments: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching tournaments.");
            }
        }

        [Authorize]
        [HttpPatch("status/{tournamentId}")]
        public async Task<IActionResult> UpdateCustomTournamentStatus(int tournamentId, [FromBody] CustomTournamentStatusUpdateDto statusUpdate)
        {
            try
            {
                var isUpdated = await _tournamentService.UpdateCustomTournamentStatusAsync(tournamentId, statusUpdate.IsActive);

                if (!isUpdated)
                {
                    return NotFound($"Tournament with ID {tournamentId} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating tournament status for ID {tournamentId}: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the tournament status.");
            }
        }

        [Authorize]
        [HttpGet("get/{tournamentId}")]
        public async Task<IActionResult> GetCustomTournamentById(int tournamentId)
        {
            return Ok();
        }

        [Authorize]
        [HttpDelete("delete/{tournamentId}")]
        public async Task<IActionResult> DeleteCustomTournamentById(int tournamentId)
        {
            return Ok();
        }

        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateCustomTournament([FromBody] CustomTournamentDto tournamentDto)
        {
            return Ok();
        }




        // create new tournament
        // update tournament
        // calculate tournament results
    }
}
