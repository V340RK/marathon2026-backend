using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Models.Dishes;
using APIHandler.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Dishes;

public class CreateDishHandler : IApiRequestHandler
{
    private static readonly string LocationsTableName = ResourceNames.LocationsTableName;
    private static readonly string DishesTableName = ResourceNames.DishesTableName;
    private static readonly string DishesImageFolderName = "assets/img/dishes";
    private readonly string _bucketName = ResourceNames.ImagesBucketName;

    private readonly S3ImageService _s3ImageService;
    private readonly IDynamoDBContext _dbContext;

    public CreateDishHandler(IDynamoDBContext dbContext, S3ImageService s3ImageService)
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

        CreateDishRequest? createDishRequest;
        try
        {
            createDishRequest = JsonSerializer.Deserialize<CreateDishRequest>(request.Body);
        }
        catch (JsonException)
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid JSON format", context.Logger);
        }

        if (createDishRequest == null)
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid payload", context.Logger);
        }

        var newDish = DishMapper.FromCreateRequest(createDishRequest);

        if (createDishRequest.ImageBase64 != null)
        {
            var fileName = $"dish_{newDish.Id}";
            var imageKey =
                await _s3ImageService.GetImageKey(createDishRequest.ImageBase64, _bucketName, DishesImageFolderName, fileName, context.Logger);

            newDish.ImageUrl = _s3ImageService.GetPublicUrl(_bucketName, imageKey);
        }

        await _dbContext.SaveAsync(newDish, DynamoConfig.SaveConfig(DishesTableName));
        context.Logger.Log($"Dish with id: {newDish.Id} was created successfully.");

        foreach (var locationId in createDishRequest.LocationIds)
        {
            var location = await _dbContext.LoadAsync<Location>(locationId, new LoadConfig()
            {
                OverrideTableName = LocationsTableName,
            });

            if (location is null)
            {
                return ResponseBuilder.CreateErrorResponse(
                    (int)HttpStatusCode.NotFound,
                    $"Location with id: {locationId} was not found",
                    context.Logger);
            }

            location.SpecialtyDishIds ??= new List<string>();
            if (!location.SpecialtyDishIds.Contains(newDish.Id))
            {
                location.SpecialtyDishIds.Add(newDish.Id);
                await _dbContext.SaveAsync(location, DynamoConfig.SaveConfig(LocationsTableName));
                context.Logger.Log($"The dish with id: {newDish.Id} was added to location with id: {locationId}.");
            }
        }

        var dishResponse = DishMapper.ToDishResponse(newDish);
        return ResponseBuilder.CreateSuccessResponse(dishResponse, 201);
    }
}