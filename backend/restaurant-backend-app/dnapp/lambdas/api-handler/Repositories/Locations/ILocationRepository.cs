using APIHandler.Entities;
using APIHandler.Models.Feedbacks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Locations;

public interface ILocationRepository
{
    Task<Location?> GetLocationByIdAsync(string locationId);
    Task<Table?> GetTableByIdAsync(string tableId);
    Task<List<Table>> GetTablesByLocationIdAsync(string locationId);
    Task<List<Feedback>> GetFeedbackByLocationIdAsync(string locationId);
    Task<IEnumerable<Location>> GetAllLocationsAsync();
    Task SaveLocationAsync(Location location);
    Task DeleteLocationAsync(Location location);
}
