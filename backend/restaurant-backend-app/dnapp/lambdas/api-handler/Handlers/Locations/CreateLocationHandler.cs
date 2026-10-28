using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Models.Locations;
using APIHandler.Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Locations;

public class CreateLocationHandler : IApiRequestHandler
{
    private static readonly string LocationsTableName = ResourceNames.LocationsTableName;
    private readonly string _bucketName = ResourceNames.ImagesBucketName;
    private static readonly string LocationsImageFolderName = "assets/img/locations";

    private readonly S3ImageService _s3ImageService;
    private readonly IDynamoDBContext _dbContext;

    public CreateLocationHandler(IDynamoDBContext dbContext, S3ImageService s3ImageService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _s3ImageService = s3ImageService ?? throw new ArgumentNullException(nameof(s3ImageService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Request body is empty", context.Logger);
        }

        CreateLocationRequest? createLocationRequest;
        try
        {
            createLocationRequest = JsonSerializer.Deserialize<CreateLocationRequest>(request.Body);
        }
        catch (JsonException)
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid JSON format", context.Logger);
        }

        if (createLocationRequest is null)
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid payload", context.Logger);
        }

        if (!OccupancyHelper.TryParseStrict(createLocationRequest.AverageOccupancy, out string sanitizedOccupancy))
        {
            return ResponseBuilder.CreateErrorResponse(
                400,
                $"Invalid Average Occupancy '{createLocationRequest.AverageOccupancy}'. Must be a numeric value between 0 and 100 (e.g., '65' or '65%').",
                context.Logger);
        }

        createLocationRequest.AverageOccupancy = sanitizedOccupancy;

        var location = LocationMapper.FromCreateRequest(createLocationRequest);

        if (createLocationRequest.ImageBase64 != null)
        {
            var fileName = $"location_{location.Id}";
            var imageKey =
                await _s3ImageService.GetImageKey(createLocationRequest.ImageBase64, _bucketName, LocationsImageFolderName, fileName, context.Logger);
            location.ImageUrl = _s3ImageService.GetPublicUrl(_bucketName, imageKey);
        }

        await _dbContext.SaveAsync(location, DynamoConfig.SaveConfig(LocationsTableName));

        var locationResponse = LocationMapper.ToLocationResponse(location);

        return ResponseBuilder.CreateSuccessResponse(locationResponse, 201);
    }
}