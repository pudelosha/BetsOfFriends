namespace Backend.Repository.Interfaces
{
    public interface ILocalizationService
    {
        string Translate(string key, string? language, IReadOnlyDictionary<string, string>? placeholders = null);
    }
}
