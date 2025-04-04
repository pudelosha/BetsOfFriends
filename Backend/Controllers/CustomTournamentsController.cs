using Backend.DTOs;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/custom-tournaments")]
    public class CustomTournamentsController : ControllerBase
    {
        private readonly ICustomTournamentService _tournamentService;
        private readonly ILogger<CustomTournamentsController> _logger;
        private readonly IUserService _userService;

        public CustomTournamentsController(ICustomTournamentService tournamentService, ILogger<CustomTournamentsController> logger, IUserService userService)
        {
            _tournamentService = tournamentService;
            _logger = logger;
            _userService = userService;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomTournament([FromBody] CustomTournamentDto tournamentDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid tournament data received.");
                return BadRequest(ModelState);
            }

            var userId = _userService.GetUserIdFromClaims(User);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized tournament creation attempt.");
                return Unauthorized("User is not authenticated.");
            }

            // Fetch User from DB (Ensure User Exists)
            var user = await _userService.FindUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User ID {userId} not found.");
                return Unauthorized("Invalid user.");
            }

            _logger.LogInformation($"User {user.Email} ({userId}) is creating a tournament.");

            // Set CreatedBy field with logged-in user ID
            tournamentDto.CreatedBy = userId;

            // Call Tournament Service
            bool success = await _tournamentService.CreateCustomTournamentAsync(tournamentDto);

            if (!success)
            {
                return StatusCode(500, "Error inserting tournament.");
            }

            return Ok(new { message = "Tournament created successfully!" });
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet]
        public async Task<IActionResult> GetAllCustomTournaments()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt: Missing user ID.");
                    return Unauthorized("User ID not found in claims.");
                }

                var tournaments = await _tournamentService.GetAllCustomTournamentsAsync(userId);
                return Ok(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching tournaments: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching tournaments.");
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPatch("status/{tournamentId}")]
        public async Task<IActionResult> UpdateCustomTournamentStatus(int tournamentId, [FromBody] CustomTournamentStatusUpdateDto statusUpdate)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt: Missing user ID.");
                    return Unauthorized("User ID not found in claims.");
                }

                var isUpdated = await _tournamentService.UpdateCustomTournamentStatusAsync(tournamentId, userId, statusUpdate.IsActive);

                if (isUpdated == null)
                {
                    return NotFound($"Tournament with ID {tournamentId} not found.");
                }
                if (!isUpdated.Value)
                {
                    return Forbid("You do not have permission to update this tournament.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating tournament status for ID {tournamentId}: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the tournament status.");
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("get/{tournamentId}")]
        public async Task<IActionResult> GetCustomTournamentById(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt: Missing user ID.");
                    return Unauthorized("User ID not found in claims.");
                }

                var tournament = await _tournamentService.GetCustomTournamentByIdAsync(tournamentId, userId);

                if (tournament == null)
                {
                    return NotFound(new { Message = $"Custom tournament with ID {tournamentId} not found or access denied." });
                }

                return Ok(tournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching the custom tournament ID {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while fetching the custom tournament." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpDelete("delete/{tournamentId}")]
        public async Task<IActionResult> DeleteCustomTournamentById(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt: Missing user ID.");
                    return Unauthorized("User ID not found in claims.");
                }

                var isDeleted = await _tournamentService.DeleteCustomTournamentByIdAsync(tournamentId, userId);

                if (isDeleted == null)
                {
                    return NotFound(new { Message = $"Tournament with ID {tournamentId} not found." });
                }
                if (!isDeleted.Value)
                {
                    return Forbid("You do not have permission to delete this tournament.");
                }

                return Ok(new { Message = $"Custom tournament with ID {tournamentId} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting the custom tournament with ID: {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while deleting the tournament." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateCustomTournament([FromBody] CustomTournamentDto tournamentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt: Missing user ID.");
                    return Unauthorized("User ID not found in claims.");
                }

                bool? success = await _tournamentService.UpdateCustomTournamentAsync(tournamentDto, userId);

                if (success == null)
                {
                    return NotFound("Tournament not found.");
                }
                if (!success.Value)
                {
                    return Forbid("You do not have permission to update this tournament.");
                }

                return Ok("Tournament updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating the custom tournament ID {tournamentDto.TournamentId}.");
                return StatusCode(500, "An error occurred while updating the tournament.");
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("my-active-tournaments")]
        public async Task<IActionResult> GetUserActiveTournaments()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt: Missing user ID.");
                    return Unauthorized("User ID not found in claims.");
                }

                var tournaments = await _tournamentService.GetUserActiveTournamentsAsync(userId);

                return Ok(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching active tournaments for the user.");
                return StatusCode(500, "An error occurred while fetching active tournaments.");
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpDelete("quit/{tournamentId}")]
        public async Task<IActionResult> QuitTournament(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized("User not found.");
                }

                var result = await _tournamentService.QuitTournamentAsync(tournamentId, userId);
                if (!result)
                {
                    return NotFound($"Tournament assignment not found for user in tournament ID {tournamentId}.");
                }

                return Ok(new { Message = "You have successfully quit the tournament." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while quitting tournament ID {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while quitting the tournament." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPatch("visibility/{tournamentId}")]
        public async Task<IActionResult> ToggleTournamentVisibility(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims.");
                    return Unauthorized("Invalid user credentials.");
                }

                _logger.LogInformation($"User {userId} is toggling visibility for tournament ID {tournamentId}");

                var updatedVisibility = await _tournamentService.ToggleTournamentVisibilityAsync(tournamentId, userId);

                if (updatedVisibility == null)
                {
                    _logger.LogWarning($"Tournament visibility toggle failed for ID {tournamentId}");
                    return NotFound($"Tournament or assignment not found.");
                }

                _logger.LogInformation($"Tournament ID {tournamentId} visibility updated to {updatedVisibility}");

                return Ok(updatedVisibility);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling visibility for tournament ID {tournamentId}");
                return StatusCode(500, "An error occurred while toggling tournament visibility.");
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("recalculate/{tournamentId}")]
        public async Task<IActionResult> RecalculateTournamentBets(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User not found." });
                }

                var success = await _tournamentService.RecalculateTournamentBetsAsync(tournamentId, userId);
                if (!success)
                {
                    return NotFound(new { Message = $"No bets found to recalculate for tournament ID {tournamentId}." });
                }

                return Ok(new { Message = "Tournament bets recalculated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while recalculating bets for tournament ID {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while recalculating bets." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("summary/{tournamentId}")]
        public async Task<IActionResult> GetTournamentSummary(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User not found." });
                }

                var summary = await _tournamentService.GetTournamentSummaryAsync(tournamentId, userId);
                if (summary == null)
                {
                    return Forbid("User is not assigned to this tournament.");
                }

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tournament summary for ID {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while retrieving the tournament summary." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("result/{tournamentId}")]
        public async Task<IActionResult> GetTournamentPlayerResult(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var summary = await _tournamentService.GetTournamentPlayerResultAsync(tournamentId, userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tournament result for ID {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while fetching the tournament result." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("betting-stats/{tournamentId}/{statsUserId}")]
        public async Task<IActionResult> GetUserBettingStats(int tournamentId, string statsUserId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var stats = await _tournamentService.GetUserBettingStatsAsync(userId, tournamentId, statsUserId);

                if (stats == null || !stats.Any())
                {
                    return NotFound(new { Message = "No betting stats found for the user in this tournament." });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching betting stats for user {statsUserId} in tournament {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while fetching betting statistics." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("invites/pending")]
        public async Task<IActionResult> GetPendingTournamentInvites()
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var invites = await _tournamentService.GetPendingTournamentInvitesAsync(userId);
                return Ok(invites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending tournament invites");
                return StatusCode(500, new { Message = "An error occurred while fetching invites." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("stages/{tournamentId}")]
        public async Task<IActionResult> GetTournamentStages(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var stages = await _tournamentService.GetTournamentStagesAsync(tournamentId, userId);
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

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("check-name")]
        public async Task<IActionResult> CheckTournamentNameAvailability([FromBody] CustomTournamentNameDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { Message = "Tournament name cannot be empty." });
                }

                _logger.LogInformation($"Checking availability for tournament name: {request.Name}");

                bool nameExists = await _tournamentService.TournamentNameExistsAsync(request.Name);

                if (nameExists)
                {
                    _logger.LogWarning($"Tournament name '{request.Name}' is already taken.");
                }
                else
                {
                    _logger.LogInformation($"Tournament name '{request.Name}' is available.");
                }

                return Ok(new { Available = !nameExists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while checking availability for tournament name: {request.Name}");
                return StatusCode(500, new { Message = "An error occurred while checking tournament name availability." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("search-public")]
        public async Task<IActionResult> SearchPublicTournaments([FromBody] TournamentSearchRequestDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                _logger.LogInformation($"User {userId} is searching public tournaments with term: '{request?.SearchTerm}'");

                var tournaments = await _tournamentService.GetPublicActiveTournamentsAsync(userId);

                // Filter by search term (optional)
                if (!string.IsNullOrWhiteSpace(request?.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.Trim().ToLower();
                    tournaments = tournaments
                        .Where(t => t.TournamentName.ToLower().Contains(searchTerm))
                        .ToList();
                }

                return Ok(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while searching public tournaments");
                return StatusCode(500, new { Message = "An error occurred while fetching tournaments." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("participants/{tournamentId}")]
        public async Task<IActionResult> GetTournamentParticipants(int tournamentId, [FromQuery] string status = "Accepted")
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var participants = await _tournamentService.GetTournamentParticipantsAsync(tournamentId, userId, status);

                if (participants == null)
                {
                    return Ok(new List<CustomUserDto>());
                }

                return Ok(participants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching participants for tournament ID {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while fetching tournament participants." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("participants/{tournamentId}/exclude")]
        public async Task<IActionResult> ExcludeParticipant(int tournamentId, [FromBody] ParticipantActionRequest request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User authentication failed." });

                var result = await _tournamentService.ExcludeParticipantAsync(tournamentId, userId, request.UserEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error excluding participant {request.UserEmail} from tournament {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while excluding the participant." });
            }
        }


        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("participants/{tournamentId}/accept")]
        public async Task<IActionResult> AcceptParticipant(int tournamentId, [FromBody] ParticipantActionRequest request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User authentication failed." });

                var result = await _tournamentService.AcceptParticipantAsync(tournamentId, userId, request.UserEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting participant {request.UserEmail} for tournament {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while accepting the participant." });
            }
        }


        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("participants/{tournamentId}/resend")]
        public async Task<IActionResult> ResendParticipantInvite(int tournamentId, [FromBody] ParticipantActionRequest request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User authentication failed." });

                var result = await _tournamentService.ResendInviteAsync(tournamentId, userId, request.UserEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending invite to {request.UserEmail} for tournament {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while resending the invitation." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("assignment/{tournamentId}")]
        public async Task<IActionResult> GetAssignmentDetails(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var assignment = await _tournamentService.GetAssignmentDetailsAsync(tournamentId, userId);

                if (assignment == null)
                {
                    return NotFound(new { Message = "Assignment not found for this tournament." });
                }

                return Ok(new { nickname = assignment.Nickname });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching assignment for user in tournament {tournamentId}");
                return StatusCode(500, new { Message = "An error occurred while retrieving assignment details." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPut("assignment/{tournamentId}")]
        public async Task<IActionResult> UpdateTournamentAssignment(int tournamentId, [FromBody] UpdateAssignmentRequest request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new TournamentInvitationResponseDto
                    {
                        Success = false,
                        Message = "User authentication failed."
                    });
                }

                var response = await _tournamentService.UpdateTournamentAssignmentAsync(tournamentId, userId, request.Nickname);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating assignment for user in tournament {tournamentId}");
                return StatusCode(500, new TournamentInvitationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while updating the assignment."
                });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("accept-invitation/{tournamentId}")]
        public async Task<IActionResult> AcceptTournamentInvitation(int tournamentId, [FromBody] TournamentInvitationRequestDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found." });
                }

                var result = await _tournamentService.AcceptTournamentInvitationAsync(tournamentId, userId, request.Nickname);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while accepting tournament invitation for ID {tournamentId}");
                return StatusCode(500, new { message = "An error occurred while accepting the tournament invitation." });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("request-join")]
        public async Task<IActionResult> RequestToJoinTournament([FromBody] TournamentJoinRequestDto request)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User authentication failed." });
                }

                var result = await _tournamentService.RequestToJoinTournamentAsync(userId, request.TournamentId, request.Nickname, request.Message);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting to join tournament.");
                return StatusCode(500, new TournamentInvitationResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while requesting to join the tournament."
                });
            }
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("pending-updates/{tournamentId}")]
        public async Task<IActionResult> CheckForPendingUpdates(int tournamentId)
        {
            try
            {
                var userId = _userService.GetUserIdFromClaims(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt: Missing user ID.");
                    return Unauthorized("User ID not found in claims.");
                }

                var updatedTournament = await _tournamentService.CheckForPendingUpdatesAsync(tournamentId, userId);

                if (updatedTournament == null)
                {
                    return NotFound(new { Message = $"Custom tournament with ID {tournamentId} not found or no updates available." });
                }

                return Ok(updatedTournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while checking updates for custom tournament ID {tournamentId}.");
                return StatusCode(500, new { Message = "An error occurred while checking for tournament updates." });
            }
        }
    }
}
