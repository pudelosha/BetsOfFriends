using Backend.DTOs;
using Backend.Repository.Interfaces;
using Backend.Repository.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/predefined-tournaments")]
    public class PredefinedTournamentController : ControllerBase
    {
        private readonly IPredefinedTournamentService _tournamentService;
        private readonly ILogger<PredefinedTournamentController> _logger;

        public PredefinedTournamentController(IPredefinedTournamentService tournamentService, ILogger<PredefinedTournamentController> logger)
        {
            _tournamentService = tournamentService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePredefinedTournament([FromBody] PredefinedTournamentDto tournamentDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid tournament data received.");
                return BadRequest(ModelState);
            }

            foreach (var match in tournamentDto.Matches)
            {
                _logger.LogInformation($"Received match: {match.MatchId}, Date: {match.MatchStart:o} (Raw: {match.MatchStart})");
            }

            bool success = await _tournamentService.CreatePredefinedTournamentAsync(tournamentDto);

            if (!success)
            {
                return StatusCode(500, "Error inserting tournament.");
            }

            return Ok(new { message = "Tournament created successfully!" });
        }


        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdatePredefinedTournament([FromBody] PredefinedTournamentDto tournamentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool success = await _tournamentService.UpdatePredefinedTournamentAsync(tournamentDto);

            if (!success)
                return NotFound("Tournament not found.");

            return Ok("Tournament updated successfully.");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllPredefinedTournaments()
        {
            try
            {
                var tournaments = await _tournamentService.GetAllPredefinedTournamentsAsync();
                return Ok(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching tournaments: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching tournaments.");
            }
        }

        [Authorize]
        [HttpGet("get/{tournamentId}")]
        public async Task<IActionResult> GetTournamentById(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Received request to fetch tournament with ID: {tournamentId}");
                var tournament = await _tournamentService.GetPredefinedTournamentByIdAsync(tournamentId);

                if (tournament == null)
                {
                    return NotFound(new { Message = $"Tournament with ID {tournamentId} not found." });
                }

                return Ok(tournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the tournament.");
                return StatusCode(500, new { Message = "An error occurred while fetching the tournament." });
            }
        }

        [Authorize]
        [HttpDelete("delete/{tournamentId}")]
        public async Task<IActionResult> DeleteTournamentById(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Received request to delete tournament with ID: {tournamentId}");

                var isDeleted = await _tournamentService.DeletePredefinedTournamentByIdAsync(tournamentId);

                if (!isDeleted)
                {
                    return NotFound(new { Message = $"Tournament with ID {tournamentId} not found or already deleted." });
                }

                return Ok(new { Message = $"Tournament with ID {tournamentId} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting the tournament with ID: {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while deleting the tournament." });
            }
        }

        [Authorize]
        [HttpPatch("status/{tournamentId}")]
        public async Task<IActionResult> UpdatePredefinedTournamentStatus(int tournamentId, [FromBody] TournamentStatusUpdateDto statusUpdate)
        {
            try
            {
                var isUpdated = await _tournamentService.UpdatePredefinedTournamentStatusAsync(tournamentId, statusUpdate.IsActive);

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
    }
}
