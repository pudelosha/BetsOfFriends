using Backend.DTOs;
using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

public class SupportService : ISupportService
{
    private readonly ILogger<SupportService> _logger;
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public SupportService(
        ILogger<SupportService> logger,
        AppDbContext context,
        INotificationService notificationService)
    {
        _logger = logger;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task HandleSupportMessageAsync(SupportMessageDto dto)
    {
        _logger.LogInformation($"Processing support message: {dto.Email}, Lang: {dto.Language}");

        var language = await _context.Languages
            .FirstOrDefaultAsync(l => l.ShortName == dto.Language);

        if (language == null)
        {
            _logger.LogWarning($"Language '{dto.Language}' not found. Using default.");
            language = await _context.Languages.FirstOrDefaultAsync(l => l.ShortName == "en");
        }

        var message = new SupportMessage
        {
            Email = dto.Email,
            Subject = dto.Subject,
            Message = dto.Message,
            LanguageId = language?.LanguageId ?? 1
        };

        _context.SupportMessages.Add(message);
        await _context.SaveChangesAsync();

        await _notificationService.NotifySuperAdminsAboutSupportMessageAsync(message);
    }
}
