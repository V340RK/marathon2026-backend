using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class AddDishToGuestSessionHandler : IApiRequestHandler
{
    private readonly IGuestSessionService _guestSessionService;
    private readonly IGuestAuthorizationService _guestAuthorizationValidator;

    public AddDishToGuestSessionHandler(IGuestSessionService guestSessionService, IGuestAuthorizationService guestAuthorizationValidator)
    {
        _guestSessionService = guestSessionService ?? throw new ArgumentNullException(nameof(guestSessionService));
        _guestAuthorizationValidator = guestAuthorizationValidator ?? throw new ArgumentNullException(nameof(guestSessionService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("guestSessionId", out string? guestSessionId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing guestSessionId in path parameters.", context.Logger);
        }

        if (!TryDeserializeRequest(request, out var updateRequest))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request body.", context.Logger);
        }

        if (string.IsNullOrWhiteSpace(updateRequest!.DishId) || updateRequest.Quantity <= 0)
        {
            return ResponseBuilder.CreateErrorResponse(400, "DishId is required and Quantity must be greater than zero.", context.Logger);
        }

        if (await _guestAuthorizationValidator.ValidateAsync(request, context, guestSessionId) is APIGatewayProxyResponse errorResponse)
            return errorResponse;

        try
        {
            await _guestSessionService.AddDishAsync(guestSessionId, updateRequest.DishId, updateRequest.Quantity);
            return ResponseBuilder.CreateSuccessResponse("Dish added successfully.");
        }
        catch (GroupReservationExpiredException exception)
        {
            return ResponseBuilder.CreateErrorResponse(409, exception.Message, context.Logger);
        }
        catch (GroupReservationSubmittedException exception)
        {
            return ResponseBuilder.CreateErrorResponse(409, exception.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorCodeResponse(ex);
        }
        catch (Exception ex)
        {
            return ResponseBuilder.CreateErrorResponse(500, $"Internal server error: {ex.Message}", context.Logger);
        }
    }

    private static bool TryDeserializeRequest(APIGatewayProxyRequest request, out UpdateGuestSessionDishRequest? updateRequest)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            updateRequest = JsonSerializer.Deserialize<UpdateGuestSessionDishRequest>(request.Body, options);
            return updateRequest != null;
        }
        catch (JsonException)
        {
            updateRequest = null;
            return false;
        }
    }
}