using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LanguageService> _logger;

        public LanguageService(AppDbContext context, ILogger<LanguageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Language?> GetByShortNameAsync(string shortName)
        {
            return await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ShortName.ToLower() == shortName.ToLower());
        }

        public async Task<Language?> GetByIdAsync(int id)
        {
            return await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LanguageId == id);
        }

        public async Task<List<Language>> GetAllLanguagesAsync()
        {
            try
            {
                _logger.LogInformation("Querying available languages from the database.");
                return await _context.Languages
                    .OrderBy(l => l.LanguageId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch languages.");
                return new List<Language>();
            }
        }
    }
}
