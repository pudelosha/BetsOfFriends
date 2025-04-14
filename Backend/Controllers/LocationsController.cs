using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILocationService _locationService;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(
            INotificationService notificationService,
            IUserService userService,
            ILocationService locationService,
            ILogger<LocationsController> logger)
        {
            _notificationService = notificationService;
            _userService = userService;
            _locationService = locationService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet]
        public async Task<ActionResult<List<LocationDto>>> GetLocations()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt to extract locations.");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var profile = await _locationService.GetAvailableCountriesAsync();
                if (profile == null)
                {
                    _logger.LogWarning($"Countries not extracted.");
                    return NotFound(new { message = "Countries not extracted." });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available countries.");
                return StatusCode(500, "Internal Server Error");
            }
        }




    }
}
