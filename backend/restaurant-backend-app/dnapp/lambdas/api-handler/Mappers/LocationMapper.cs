using APIHandler.Entities;
using APIHandler.Models.Locations;
using System;

namespace APIHandler.Mappers;

public static class LocationMapper
{
    public static GetLocationResponse ToLocationResponse(Location location)
    {
        return new GetLocationResponse
        {
            Id = location.Id,
            Address = location.Address,
            AverageOccupancy = location.AverageOccupancy,
            Description = location.Description,
            ImageUrl = string.IsNullOrWhiteSpace(location.ImageUrl)
                ? null
                : location.ImageUrl,
            Rating = location.Rating,
            TotalCapacity = location.TotalCapacity,
        };
    }

    public static Location FromCreateRequest(CreateLocationRequest createLocationRequest)
    {
        return new Location
        {
            Id = Guid.NewGuid().ToString(),
            Address = createLocationRequest.Address,
            Description = createLocationRequest.Description,
            TotalCapacity = createLocationRequest.TotalCapacity,
            AverageOccupancy = createLocationRequest.AverageOccupancy,
            Rating = createLocationRequest.Rating
        };
    }
}