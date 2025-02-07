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
    }
}
