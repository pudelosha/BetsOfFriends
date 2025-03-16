using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Services.Interfaces;
using Backend.Model.Entities;
using Backend.DTOs;
using Backend.Repository.Interfaces;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            IUserService userService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestNotifications()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request to fetch notifications.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                _logger.LogInformation($"Fetching notifications for user {userId}");

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, 5);

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user notifications.");
                return StatusCode(500, new { Message = "An error occurred while fetching notifications." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request to mark notification as read.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                _logger.LogInformation($"User {userId} is marking notification {notificationId} as read");

                var success = await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);

                if (!success)
                {
                    _logger.LogWarning($"Notification {notificationId} not found or does not belong to user {userId}");
                    return NotFound(new { Message = "Notification not found or already read." });
                }

                return Ok(new { Message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {notificationId} as read.");
                return StatusCode(500, new { Message = "An error occurred while marking the notification as read." });
            }
        }
    }
}
