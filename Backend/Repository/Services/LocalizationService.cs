using System.Collections.Concurrent;
using System.Text.Json;
using Backend.Repository.Interfaces;

namespace Backend.Repository.Services
{
    public class LocalizationService : ILocalizationService
    {
        private const string DefaultLanguage = "en";
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
        private readonly ILogger<LocalizationService> _logger;

        public LocalizationService(ILogger<LocalizationService> logger)
        {
            _logger = logger;
        }

        public string Translate(string key, string? language, IReadOnlyDictionary<string, string>? placeholders = null)
        {
            var normalizedLanguage = NormalizeLanguage(language);
            var translations = LoadTranslations(normalizedLanguage);

            if (!translations.TryGetValue(key, out var value))
            {
                translations = LoadTranslations(DefaultLanguage);

                if (!translations.TryGetValue(key, out value))
                {
                    _logger.LogWarning("Missing translation key {Key} for language {Language}.", key, normalizedLanguage);
                    return ApplyPlaceholders(key, placeholders);
                }
            }

            return ApplyPlaceholders(value, placeholders);
        }

        private Dictionary<string, string> LoadTranslations(string language)
        {
            return _cache.GetOrAdd(language, lang =>
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Localization", $"{lang}.json");

                if (!File.Exists(path) && lang != DefaultLanguage)
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Localization", $"{DefaultLanguage}.json");
                }

                if (!File.Exists(path))
                {
                    _logger.LogWarning("Localization file not found for language {Language}.", lang);
                    return new Dictionary<string, string>();
                }

                using var stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);

                var flattened = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Flatten(document.RootElement, string.Empty, flattened);

                return flattened;
            });
        }

        private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> target)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrWhiteSpace(prefix)
                    ? property.Name
                    : $"{prefix}.{property.Name}";

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    Flatten(property.Value, key, target);
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    target[key] = property.Value.GetString() ?? string.Empty;
                }
            }
        }

        private static string ApplyPlaceholders(string value, IReadOnlyDictionary<string, string>? placeholders)
        {
            if (placeholders == null)
            {
                return value;
            }

            foreach (var placeholder in placeholders)
            {
                value = value.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value ?? string.Empty);
            }

            return value;
        }

        private static string NormalizeLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return DefaultLanguage;
            }

            return language.Trim().ToLowerInvariant().Split('-')[0];
        }
    }
}
