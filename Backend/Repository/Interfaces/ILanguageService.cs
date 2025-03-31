using Backend.Model.Entities;

namespace Backend.Repository.Interfaces
{
    public interface ILanguageService
    {
        Task<Language?> GetByShortNameAsync(string shortName);
        Task<Language?> GetByIdAsync(int id);
        Task<List<Language>> GetAllLanguagesAsync();
    }
}
