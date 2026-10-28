using APIHandler.Entities;
using APIHandler.Models.Locations;
using System.Threading.Tasks;

namespace APIHandler.Services.Locations;

public interface ILocationService
{
    Task<Location> GetLocationByTableIdAsync(string tableId);

    Task<int> GetLocationCapacityAsync(string locationId);

    Task<double> GetLocationRatingAsync(string locationId);

    Task<Location?> GetLocationByIdAsync(string locationId);

    Task<Location> UpdateLocationAsync(string locationId, CreateLocationRequest request, string? newImageUrl);
    Task RemoveDishFromLocationAsync(string locationId, string dishId);

    Task DeleteLocationAsync(string locationId, bool force = false);
}