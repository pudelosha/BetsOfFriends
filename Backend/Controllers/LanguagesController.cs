using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LanguagesController : ControllerBase
    {
        private readonly ILanguageService _languageService;
        private readonly ILogger<LanguagesController> _logger;

        public LanguagesController(ILanguageService languageService, ILogger<LanguagesController> logger)
        {
            _languageService = languageService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the list of available languages.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetLanguages()
        {
            try
            {
                _logger.LogInformation("Fetching available languages...");

                var languages = await _languageService.GetAllLanguagesAsync();

                if (languages == null || !languages.Any())
                {
                    _logger.LogWarning("No languages found in the database.");
                    return NotFound(new { Success = false, Message = "No languages available." });
                }

                _logger.LogInformation($"Returning {languages.Count} available languages.");
                return Ok(languages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving languages.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}
