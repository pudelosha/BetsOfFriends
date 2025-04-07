using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalDataController : ControllerBase
    {
        private readonly IFootballDataService _footballService;
        private readonly ILogger<ExternalDataController> _logger;

        public ExternalDataController(IFootballDataService footballService, ILogger<ExternalDataController> logger)
        {
            _footballService = footballService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("competition/{competitionCode}/season/{seasonCode}")]
        public async Task<IActionResult> GetCompetitionMatches(int competitionCode, int seasonCode)
        {
            try
            {
                var json = await _footballService.GetCompetitionMatchesAsync(competitionCode, seasonCode);
                var tournamentData = await _footballService.ConvertToPredefinedTournamentDtoAsync(json);
                return Ok(tournamentData);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch matches for competition {CompetitionCode} and season {SeasonCode}", competitionCode, seasonCode);
                return StatusCode(502, "Failed to retrieve match data from external API.");
            }
        }
    }
}
