using Backend.DTOs;
using Backend.Repository.Interfaces;
using Backend.Repository.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("predefined-tournaments")]
    public class PredefinedTournamentsController : ControllerBase
    {
        private readonly IPredefinedTournamentService _tournamentService;
        private readonly ILogger<PredefinedTournamentsController> _logger;

        public PredefinedTournamentsController(IPredefinedTournamentService tournamentService, ILogger<PredefinedTournamentsController> logger)
        {
            _tournamentService = tournamentService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePredefinedTournament([FromBody] PredefinedTournamentDto tournamentDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid tournament data received.");
                return BadRequest(ModelState);
            }

            var qualificationOddsError = GetQualificationOddsValidationError(tournamentDto);
            if (qualificationOddsError != null)
            {
                _logger.LogWarning(qualificationOddsError);
                return BadRequest(new { message = qualificationOddsError });
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdatePredefinedTournament([FromBody] PredefinedTournamentDto tournamentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var qualificationOddsError = GetQualificationOddsValidationError(tournamentDto);
            if (qualificationOddsError != null)
            {
                _logger.LogWarning(qualificationOddsError);
                return BadRequest(new { message = qualificationOddsError });
            }

            bool success = await _tournamentService.UpdatePredefinedTournamentAsync(tournamentDto);

            if (!success)
                return NotFound("Tournament not found.");

            return Ok("Tournament updated successfully.");
        }

        private static string? GetQualificationOddsValidationError(PredefinedTournamentDto tournamentDto)
        {
            foreach (var match in tournamentDto.Matches.Where(m => m.RecordStatus != "Delete"))
            {
                if (!IsQualificationMatch(match.MatchType)) continue;

                if (match.HomeQualifies is > 0 && match.AwayQualifies is > 0) continue;

                var label = match.MatchId.HasValue
                    ? $"match ID {match.MatchId.Value}"
                    : $"{match.HomeTeam} vs {match.AwayTeam}";

                return $"Qualification odds must be greater than zero for {label}.";
            }

            return null;
        }

        private static bool IsQualificationMatch(string matchType)
        {
            return string.Equals(matchType, "ExtendedWithQualification", StringComparison.OrdinalIgnoreCase);
        }

        [Authorize(Roles = "SuperAdmin")]
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

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("get/{tournamentId}")]
        public async Task<IActionResult> GetPredefinedTournamentById(int tournamentId)
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("delete/{tournamentId}")]
        public async Task<IActionResult> DeletePredefinedTournamentById(int tournamentId)
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

        [Authorize(Roles = "SuperAdmin")]
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("recalculate-linked/{tournamentId}")]
        public async Task<IActionResult> RecalculateLinkedCustomTournamentBets(int tournamentId)
        {
            try
            {
                var recalculatedCount = await _tournamentService.RecalculateLinkedCustomTournamentBetsAsync(tournamentId);

                if (recalculatedCount == null)
                {
                    return NotFound(new { Message = $"Tournament with ID {tournamentId} not found." });
                }

                return Ok(new
                {
                    Message = "Linked custom tournament bets recalculated successfully.",
                    RecalculatedTournaments = recalculatedCount.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recalculating linked custom tournaments for predefined tournament ID {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while recalculating linked custom tournament bets." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePredefinedTournaments()
        {
            try
            {
                _logger.LogInformation("Fetching active predefined tournaments.");
                var tournaments = await _tournamentService.GetActivePredefinedTournamentsAsync();
                return Ok(tournaments);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError($"Application error while fetching tournaments: {ex.Message}", ex);
                return StatusCode(500, "An internal error occurred while retrieving predefined tournaments.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}", ex);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("stages/{tournamentId}")]
        public async Task<IActionResult> GetTournamentStages(int tournamentId)
        {
            try
            {
                var stages = await _tournamentService.GetTournamentStagesAsync(tournamentId);
                if (stages == null)
                {
                    return NotFound(new { Message = "Tournament not found or user not a participant." });
                }

                return Ok(stages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching stages for tournament ID {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while fetching tournament stages." });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("export-matches")]
        public async Task<IActionResult> ExportMatchesToExcel([FromQuery] int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Generating Excel file for tournament ID {tournamentId}.");

                var fileContent = await _tournamentService.ExportMatchesToExcelAsync(tournamentId);
                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning($"No data found or failed to generate Excel for tournament ID {tournamentId}.");
                    return NotFound("Excel export failed or tournament has no matches.");
                }

                _logger.LogInformation($"Excel export successful for tournament ID {tournamentId}.");
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "matches.xlsx");
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, $"Application error while exporting Excel for tournament ID {tournamentId}: {ex.Message}");
                return StatusCode(500, "An internal error occurred while exporting the Excel file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while exporting Excel for tournament ID {tournamentId}: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("upcoming-stage/{tournamentId}")]
        public async Task<IActionResult> GetFirstStageWithUpcomingMatches(int tournamentId)
        {
            try
            {
                _logger.LogInformation($"Fetching first stage with upcoming matches for tournament {tournamentId}");

                var stage = await _tournamentService.GetFirstStageWithUpcomingMatchesAsync(tournamentId);

                if (stage == null)
                {
                    return NoContent();
                }

                return Ok(stage);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, $"App error while fetching stage for upcoming matches in tournament {tournamentId}");
                return StatusCode(500, "An error occurred while retrieving the stage.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching stage for tournament {tournamentId}: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
