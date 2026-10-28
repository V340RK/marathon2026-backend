using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Models.Dishes;
using APIHandler.Services;
using APIHandler.Services.Dishes;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Dishes;

public class EditDishHandler(IDishService dishService, S3ImageService s3ImageService) : IApiRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var dishId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'id' in path parameters.", context.Logger);

        if (string.IsNullOrWhiteSpace(request.Body))
            return ResponseBuilder.CreateErrorResponse(400, "Request body is empty", context.Logger);

        CreateDishRequest? updateRequest;
        try
        {
            updateRequest = JsonSerializer.Deserialize<CreateDishRequest>(request.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, $"Invalid JSON format: {ex.Message}", context.Logger);
        }

        if (updateRequest is null) return ResponseBuilder.CreateErrorResponse(400, "Invalid payload", context.Logger);

        string? newImageUrl = null;
        if (!string.IsNullOrWhiteSpace(updateRequest.ImageBase64))
        {
            var imageKey = await s3ImageService.GetImageKey(
                updateRequest.ImageBase64,
                ResourceNames.ImagesBucketName,
                "assets/img/dishes",
                $"dish_{dishId}",
                context.Logger);
            newImageUrl = s3ImageService.GetPublicUrl(ResourceNames.ImagesBucketName, imageKey);
        }

        try
        {
            var updatedDish = await dishService.UpdateDishAsync(dishId, updateRequest, newImageUrl);
            return ResponseBuilder.CreateSuccessResponse(DishMapper.ToDishResponse(updatedDish));
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }
}