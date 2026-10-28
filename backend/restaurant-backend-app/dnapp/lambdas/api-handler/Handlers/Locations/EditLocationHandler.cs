using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Models.Locations;
using APIHandler.Services;
using APIHandler.Services.Locations;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Locations;

public class EditLocationHandler : IApiRequestHandler
{
    private readonly string _bucketName = ResourceNames.ImagesBucketName;
    private static readonly string LocationsImageFolderName = "assets/img/locations";

    private readonly ILocationService _locationService;
    private readonly S3ImageService _s3ImageService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EditLocationHandler(ILocationService locationService, S3ImageService s3ImageService)
    {
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _s3ImageService = s3ImageService ?? throw new ArgumentNullException(nameof(s3ImageService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var locationId) || string.IsNullOrWhiteSpace(locationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'id' in path parameters.", context.Logger);
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Request body is empty", context.Logger);
        }

        CreateLocationRequest? updateRequest;
        try
        {
            updateRequest = JsonSerializer.Deserialize<CreateLocationRequest>(request.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, $"Invalid JSON format: {ex.Message}", context.Logger);
        }

        if (updateRequest is null)
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid payload", context.Logger);
        }

        if (!OccupancyHelper.TryParseStrict(updateRequest.AverageOccupancy, out string sanitizedOccupancy))
        {
            return ResponseBuilder.CreateErrorResponse(
                400,
                $"Invalid Average Occupancy '{updateRequest.AverageOccupancy}'. Must be a numeric value between 0 and 100 (e.g., '65' or '65%').",
                context.Logger);
        }

        updateRequest.AverageOccupancy = sanitizedOccupancy;

        string? newImageUrl = null;
        if (!string.IsNullOrWhiteSpace(updateRequest.ImageBase64))
        {
            var fileName = $"location_{locationId}";
            var imageKey = await _s3ImageService.GetImageKey(updateRequest.ImageBase64, _bucketName, LocationsImageFolderName, fileName, context.Logger);
            newImageUrl = _s3ImageService.GetPublicUrl(_bucketName, imageKey);
        }

        try
        {
            var updatedLocation = await _locationService.UpdateLocationAsync(locationId, updateRequest, newImageUrl);
            var locationResponse = LocationMapper.ToLocationResponse(updatedLocation);
            return ResponseBuilder.CreateSuccessResponse(locationResponse);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error updating location {locationId}: {ex.Message}\n{ex.StackTrace}");
            return ResponseBuilder.CreateErrorResponse(500, "An internal error occurred while updating the location.", context.Logger);
        }
    }
}