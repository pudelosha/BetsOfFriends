using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface ILocationService
    {
        Task<List<LocationDto>> GetAvailableCountriesAsync();
        Task<LocationDto?> GetLocationByIdAsync(int? locationId);

    }
}
