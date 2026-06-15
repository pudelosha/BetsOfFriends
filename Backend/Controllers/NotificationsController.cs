using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Services.Interfaces;
using Backend.Model.Entities;
using Backend.DTOs;
using Backend.Repository.Interfaces;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IUserService _userService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            IPushNotificationService pushNotificationService,
            IUserService userService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _pushNotificationService = pushNotificationService;
            _userService = userService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet]
        public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found.");
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving user notifications: {ex.Message}");
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("tournament-message-recipients/{tournamentId}")]
        public async Task<ActionResult<List<NotificationMessageRecipientDto>>> GetTournamentMessageRecipients(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var recipients = await _notificationService.GetTournamentMessageRecipientsAsync(tournamentId, userId);
                return Ok(recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tournament message recipients for tournament {TournamentId}.", tournamentId);
                return StatusCode(500, new { Message = "An error occurred while fetching message recipients." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("tournament-user-message")]
        public async Task<IActionResult> SendTournamentUserMessage([FromBody] SendTournamentUserMessageDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var success = await _notificationService.SendTournamentUserMessageAsync(request, userId);
                if (!success)
                {
                    return BadRequest(new { Message = "Message could not be sent." });
                }

                return Ok(new { Message = "Message sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tournament user message for tournament {TournamentId}.", request.TournamentId);
                return StatusCode(500, new { Message = "An error occurred while sending the message." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("reply-user-message")]
        public async Task<IActionResult> ReplyToUserMessage([FromBody] ReplyToUserMessageDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var success = await _notificationService.ReplyToUserMessageAsync(request, userId);
                if (!success)
                {
                    return BadRequest(new { Message = "Reply could not be sent." });
                }

                return Ok(new { Message = "Reply sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to user message for recipient {RecipientUserId}.", request.RecipientUserId);
                return StatusCode(500, new { Message = "An error occurred while sending the reply." });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("admin-broadcast-message")]
        public async Task<IActionResult> SendAdminBroadcastMessage([FromBody] SendAdminBroadcastMessageDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var recipientCount = await _notificationService.SendAdminBroadcastMessageAsync(request, userId);
                if (recipientCount <= 0)
                {
                    return BadRequest(new { Message = "Message could not be sent." });
                }

                return Ok(new { Message = "Message sent successfully.", RecipientCount = recipientCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin broadcast message.");
                return StatusCode(500, new { Message = "An error occurred while sending the message." });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("admin-user-message")]
        public async Task<IActionResult> SendAdminUserMessage([FromBody] SendAdminUserMessageDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var success = await _notificationService.SendAdminUserMessageAsync(request, userId);
                if (!success)
                {
                    return BadRequest(new { Message = "Message could not be sent." });
                }

                return Ok(new { Message = "Message sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin user message to {RecipientUserId}.", request.RecipientUserId);
                return StatusCode(500, new { Message = "An error occurred while sending the message." });
            }
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
        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request to delete all notifications.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var deletedCount = await _notificationService.DeleteAllNotificationsAsync(userId);
                return Ok(new { DeletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all notifications.");
                return StatusCode(500, new { Message = "An error occurred while deleting notifications." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPut("mark-as-read/{notificationId}")]
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

                _logger.LogInformation($"User {userId} is marking notification {notificationId} as read.");

                var success = await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);

                if (!success)
                {
                    _logger.LogWarning($"Notification {notificationId} not found or does not belong to user {userId}.");
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

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpDelete("delete/{notificationId}")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized request to delete notification.");
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                _logger.LogInformation($"User {userId} is deleting notification {notificationId}.");

                var success = await _notificationService.DeleteNotificationAsync(notificationId, userId);

                if (!success)
                {
                    _logger.LogWarning($"Notification {notificationId} not found or does not belong to user {userId}.");
                    return NotFound(new { Message = "Notification not found or already deleted." });
                }

                return Ok(new { Message = "Notification deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting notification {notificationId}.");
                return StatusCode(500, new { Message = "An error occurred while deleting the notification." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("settings")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var settings = await _notificationService.GetNotificationSettingsAsync(userId);

                if (settings == null)
                {
                    return NotFound(new { Message = "Notification settings not found." });
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification settings.");
                return StatusCode(500, new { Message = "An error occurred while retrieving settings." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] NotificationSettingsDto settingsDto)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var updated = await _notificationService.UpdateNotificationSettingsAsync(userId, settingsDto);

                if (!updated)
                {
                    return BadRequest(new { Message = "Failed to update notification settings." });
                }

                return Ok(new { Message = "Notification settings updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings.");
                return StatusCode(500, new { Message = "An error occurred while updating settings." });
            }
        }

        [AllowAnonymous]
        [HttpGet("push/public-key")]
        public IActionResult GetPushPublicKey()
        {
            return Ok(new PushPublicKeyDto
            {
                Enabled = _pushNotificationService.IsConfigured,
                PublicKey = _pushNotificationService.GetPublicKey()
            });
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("push/subscriptions")]
        public async Task<IActionResult> SavePushSubscription([FromBody] PushSubscriptionDto subscription)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                await _pushNotificationService.UpsertSubscriptionAsync(userId, subscription);
                return Ok(new { Message = "Push subscription saved successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving push subscription.");
                return StatusCode(500, new { Message = "An error occurred while saving push subscription." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpDelete("push/subscriptions")]
        public async Task<IActionResult> DeletePushSubscription([FromBody] PushSubscriptionDeleteDto subscription)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                await _pushNotificationService.RemoveSubscriptionAsync(userId, subscription);
                return Ok(new { Message = "Push subscription deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting push subscription.");
                return StatusCode(500, new { Message = "An error occurred while deleting push subscription." });
            }
        }
    }
}
