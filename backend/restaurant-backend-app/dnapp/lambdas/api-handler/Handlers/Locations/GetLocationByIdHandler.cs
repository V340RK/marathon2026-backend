using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Services.Locations;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Locations;

public class GetLocationByIdHandler : IApiRequestHandler
{
    private static readonly string LocationsTableName = ResourceNames.LocationsTableName;

    private readonly IDynamoDBContext _dbContext;
    private readonly ILocationService _locationService;

    public GetLocationByIdHandler(IDynamoDBContext dbContext, ILocationService locationService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters == null ||
            !request.PathParameters.TryGetValue("id", out var locationId) ||
            string.IsNullOrWhiteSpace(locationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "No location id was found in request.", context.Logger);
        }

        var location = await _dbContext.LoadAsync<Location>(locationId, DynamoConfig.LoadConfig(LocationsTableName));

        if (location == null)
        {
            return ResponseBuilder.CreateErrorResponse(404, $"Location with id '{locationId}' was not found.", context.Logger);
        }

        var locationResponse = LocationMapper.ToLocationResponse(location);
        locationResponse.TotalCapacity = await _locationService.GetLocationCapacityAsync(location.Id);
        locationResponse.Rating = await _locationService.GetLocationRatingAsync(location.Id);
        locationResponse.AverageOccupancy = OccupancyHelper.SanitizeLegacy(locationResponse.AverageOccupancy);
        return ResponseBuilder.CreateSuccessResponse(locationResponse);
    }
}