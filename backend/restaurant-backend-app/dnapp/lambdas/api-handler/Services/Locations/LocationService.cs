using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.Locations;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.Waiters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.Locations;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IWaiterService _waiterService;
    private readonly ITableRepository _tableRepository;

    public LocationService(
        ILocationRepository locationRepository,
        IWaiterService waiterService,
        ITableRepository tableRepository)
    {
        _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        _waiterService = waiterService ?? throw new ArgumentNullException(nameof(waiterService));
        _tableRepository = tableRepository ?? throw new ArgumentNullException(nameof(tableRepository));
    }

    public async Task<Location> GetLocationByTableIdAsync(string tableId)
    {
        var table = await _locationRepository.GetTableByIdAsync(tableId);
        if (table is null)
        {
            throw new ValidationException($"Table with ID {tableId} not found.");
        }

        var location = await _locationRepository.GetLocationByIdAsync(table.LocationId);
        if (location is null)
        {
            throw new ValidationException($"Location with ID {table.LocationId} not found.");
        }

        return location;
    }

    public async Task<Location?> GetLocationByIdAsync(string locationId)
    {
        return await _locationRepository.GetLocationByIdAsync(locationId);
    }

    public async Task<int> GetLocationCapacityAsync(string locationId)
    {
        var tables = await _locationRepository.GetTablesByLocationIdAsync(locationId);
        return tables.Count;
    }

    public async Task<double> GetLocationRatingAsync(string locationId)
    {
        var feedbacks = await _locationRepository.GetFeedbackByLocationIdAsync(locationId);
        return feedbacks.Count == 0
            ? 0
            : Math.Round(feedbacks.Sum(f => f.Rating) / (double)feedbacks.Count, 2);
    }

    public async Task RemoveDishFromLocationAsync(string locationId, string dishId)
    {
        var location = await _locationRepository.GetLocationByIdAsync(locationId)
            ?? throw new ValidationException($"Location '{locationId}' not found.");

        if (location.SpecialtyDishIds != null && location.SpecialtyDishIds.Contains(dishId))
        {
            location.SpecialtyDishIds.Remove(dishId);
            await _locationRepository.SaveLocationAsync(location);
        }
        else
        {
            throw new ValidationException($"Dish '{dishId}' is not currently on the menu for this location.");
        }
    }

    public async Task<Location> UpdateLocationAsync(string locationId, CreateLocationRequest request, string? newImageUrl)
    {
        var existingLocation = await _locationRepository.GetLocationByIdAsync(locationId);
        if (existingLocation == null)
        {
            throw new ValidationException($"Location with id '{locationId}' was not found.");
        }

        existingLocation.Address = request.Address;
        existingLocation.Description = request.Description;
        existingLocation.TotalCapacity = request.TotalCapacity;
        existingLocation.AverageOccupancy = request.AverageOccupancy;

        if (!string.IsNullOrWhiteSpace(newImageUrl))
        {
            existingLocation.ImageUrl = newImageUrl;
        }

        await _locationRepository.SaveLocationAsync(existingLocation);
        return existingLocation;
    }

    public async Task DeleteLocationAsync(string locationId, bool force = false)
    {
        var existingLocation = await _locationRepository.GetLocationByIdAsync(locationId);
        if (existingLocation == null)
        {
            throw new ValidationException($"Location with id '{locationId}' was not found.");
        }

        var tables = await _locationRepository.GetTablesByLocationIdAsync(locationId);
        var waiters = await _waiterService.GetWaitersByLocationIdAsync(locationId);

        bool hasDependencies = (tables != null && tables.Any()) || (waiters != null && waiters.Any());

        if (hasDependencies && !force)
        {
            throw new ValidationException(
                $"Cannot delete location '{locationId}' because it has active tables or assigned waiters. Append '?force=true' to the URL to delete them automatically.");
        }

        if (force)
        {
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    await _tableRepository.DeleteTableAsync(table);
                }
            }

            if (waiters != null)
            {
                foreach (var waiter in waiters)
                {
                    await _waiterService.RemoveWaiterByIdAsync(waiter.UserId);
                }
            }
        }

        await _locationRepository.DeleteLocationAsync(existingLocation);
    }
}