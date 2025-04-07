using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly ILogger<SupportController> _logger;
        private readonly ISupportService _supportService;

        public SupportController(ILogger<SupportController> logger, ISupportService supportService)
        {
            _logger = logger;
            _supportService = supportService;
        }

        [HttpPost("contact")]
        [AllowAnonymous]
        public async Task<IActionResult> Contact([FromBody] SupportMessageDto dto)
        {
            try
            {
                _logger.LogInformation($"Support message received from {dto.Email} | Subject: {dto.Subject}");

                await _supportService.HandleSupportMessageAsync(dto);

                return Ok(new { success = true, message = "Message received." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while submitting support message");
                return StatusCode(500, new { success = false, message = "Something went wrong." });
            }
        }
    }
}
