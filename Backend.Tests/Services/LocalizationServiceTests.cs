using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class LocalizationServiceTests
{
    [Fact]
    public void Translate_ReplacesPlaceholdersInNestedKeys()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

        var result = service.Translate(
            "Notifications.NewGame.Message",
            "en",
            new Dictionary<string, string>
            {
                ["HOME_TEAM"] = "Team A",
                ["AWAY_TEAM"] = "Team B"
            });

        Assert.Equal("Team A vs Team B is now open for betting.", result);
    }

    [Fact]
    public void Translate_NormalizesRegionalLanguageCodes()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

        var result = service.Translate("Notifications.DailyUpdate.Title", "pl-PL");

        Assert.Equal("Codzienna aktualizacja turnieju", result);
    }

    [Fact]
    public void Translate_WithUnsupportedLanguageFallsBackToEnglish()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

        var result = service.Translate("Notifications.NewGame.Title", "xx");

        Assert.Equal("New match available", result);
    }

    [Fact]
    public void Translate_WithMissingKey_ReturnsKeyWithAppliedPlaceholders()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

        var result = service.Translate(
            "Missing.{{VALUE}}.Key",
            "en",
            new Dictionary<string, string> { ["VALUE"] = "Translation" });

        Assert.Equal("Missing.Translation.Key", result);
    }
}
