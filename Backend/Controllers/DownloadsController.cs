using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadsController : ControllerBase
    {
        private readonly ILogger<DownloadsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DownloadsController(ILogger<DownloadsController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        //TODO adjust later yo yo!
        //Files are expected in wwwroot/downloads/ (ensure this folder exists).
        //IWebHostEnvironment.WebRootPath gives you access to wwwroot.

        [AllowAnonymous]
        [HttpGet("{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    _logger.LogWarning("Empty filename received for download.");
                    return BadRequest(new { Message = "Filename is required." });
                }

                // Location of downloadable files (e.g., wwwroot/downloads)
                var downloadFolder = Path.Combine(_environment.WebRootPath, "downloads");
                var filePath = Path.Combine(downloadFolder, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found: {fileName}");
                    return NotFound(new { Message = $"File '{fileName}' not found." });
                }

                var mimeType = GetMimeType(fileName);
                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                _logger.LogInformation($"Serving file download: {fileName}");

                return File(fileBytes, mimeType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error serving file '{fileName}'");
                return StatusCode(500, new { Message = "An unexpected error occurred while downloading the file." });
            }
        }

        private string GetMimeType(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() switch
            {
                ".apk" => "application/vnd.android.package-archive",
                ".ipa" => "application/octet-stream", // iOS packages are often treated as binary
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}
