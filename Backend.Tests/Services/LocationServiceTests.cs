using Backend.Repository.Interfaces;
using Backend.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Services;

public class LocationServiceTests
{
    [Fact]
    public async Task GetAvailableCountriesAsync_ReturnsSeededCountriesAlphabetically()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocationService>();

        var countries = await service.GetAvailableCountriesAsync();

        Assert.Equal(249, countries.Count);
        Assert.Equal("Afghanistan", countries.First().Name);
        Assert.Equal(countries.OrderBy(country => country.Name).Select(country => country.Name), countries.Select(country => country.Name));
    }

    [Fact]
    public async Task GetLocationByIdAsync_WithKnownId_ReturnsLocationDto()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocationService>();

        var location = await service.GetLocationByIdAsync(235);

        Assert.NotNull(location);
        Assert.Equal(235, location!.CountryId);
        Assert.Equal("United States", location.Name);
    }

    [Fact]
    public async Task GetLocationByIdAsync_WithNullId_ReturnsNull()
    {
        using var host = new BackendTestHost();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocationService>();

        var location = await service.GetLocationByIdAsync(null);

        Assert.Null(location);
    }
}
