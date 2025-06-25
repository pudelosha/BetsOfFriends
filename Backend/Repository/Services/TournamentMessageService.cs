using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Services
{
    public class TournamentMessageService : ITournamentMessageService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TournamentMessageService> _logger;


        public TournamentMessageService(AppDbContext context, ILogger<TournamentMessageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TournamentMessageDto>> GetLatestMessagesAsync(int tournamentId, string userId, int count)
        {
            var isParticipant = await _context.CustomTournamentUserAssignments
                .AnyAsync(a => a.TournamentId == tournamentId
                            && a.UserId == userId
                            && a.Status == AssignmentStatus.Accepted);

            if (!isParticipant)
            {
                _logger.LogWarning($"User {userId} is not an accepted participant in tournament {tournamentId}.");
                return new List<TournamentMessageDto>();
            }

            var messages = await _context.TournamentMessages
                .Where(m => m.TournamentId == tournamentId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .Include(m => m.User)
                .ToListAsync();

            return messages.Select(m => new TournamentMessageDto
            {
                Id = m.Id,
                AuthorName = m.User?.UserName ?? "Unknown",
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        public async Task<CreateMessageResultDto> CreateMessageAsync(int tournamentId, string userId, string content)
        {
            var isParticipant = await _context.CustomTournamentUserAssignments
                .AnyAsync(a => a.TournamentId == tournamentId
                            && a.UserId == userId
                            && a.Status == AssignmentStatus.Accepted);

            if (!isParticipant)
            {
                _logger.LogWarning($"User {userId} is not an accepted participant in tournament {tournamentId}.");
                return new CreateMessageResultDto
                {
                    Success = false,
                    ErrorMessage = "You are not allowed to post messages in this tournament."
                };
            }

            var lastMessage = await _context.TournamentMessages
                .Where(m => m.TournamentId == tournamentId && m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastMessage != null)
            {
                var minutesSinceLastPost = (DateTime.UtcNow - lastMessage.CreatedAt).TotalMinutes;
                //if (minutesSinceLastPost < 5)
                //{
                //    return new CreateMessageResultDto
                //    {
                //        Success = false,
                //        ErrorMessage = $"You can post a new message after {Math.Ceiling(5 - minutesSinceLastPost)} minute(s)."
                //    };
                //}
            }

            var newMessage = new TournamentMessage
            {
                TournamentId = tournamentId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.TournamentMessages.Add(newMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} posted message in tournament {tournamentId}.");
            return new CreateMessageResultDto { Success = true };
        }
    }
}
