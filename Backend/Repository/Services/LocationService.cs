using Backend.DTOs;
using Backend.Model.Database;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Services
{
    public class LocationService : ILocationService
    {
        private readonly AppDbContext _context;

        public LocationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LocationDto>> GetAvailableCountriesAsync()
        {
            return await _context.Locations
                .OrderBy(c => c.Name)
                .Select(c => new LocationDto
                {
                    CountryId = c.LocationId,
                    Name = c.Name
                })
                .ToListAsync();
        }

        public async Task<LocationDto?> GetLocationByIdAsync(int? locationId)
        {
            if (locationId == null)
                return null;

            var location = await _context.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LocationId == locationId);

            if (location == null)
                return null;

            return new LocationDto
            {
                CountryId = location.LocationId,
                Name = location.Name
            };
        }
    }
}
