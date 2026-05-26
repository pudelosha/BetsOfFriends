using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class LanguageServiceTests
{
    [Fact]
    public async Task GetByShortNameAsync_FindsLanguageCaseInsensitively()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILanguageService>();

        var language = await service.GetByShortNameAsync("PL");

        Assert.NotNull(language);
        Assert.Equal(2, language!.LanguageId);
        Assert.Equal("pl", language.ShortName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenLanguageDoesNotExist_ReturnsNull()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILanguageService>();

        var language = await service.GetByIdAsync(999);

        Assert.Null(language);
    }

    [Fact]
    public async Task GetAllLanguagesAsync_ReturnsSeededLanguagesInConfiguredOrder()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILanguageService>();

        var languages = await service.GetAllLanguagesAsync();

        Assert.Equal(7, languages.Count);
        Assert.Equal(new[] { "en", "pl", "de", "fr", "es", "it", "pt" }, languages.Select(l => l.ShortName));
    }
}
