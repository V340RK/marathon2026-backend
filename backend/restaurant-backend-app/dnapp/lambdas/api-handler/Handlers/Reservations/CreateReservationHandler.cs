using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Reservations;
using APIHandler.Services.Reservations;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Reservations;

public class CreateReservationHandler : IApiRequestHandler
{
    private readonly IReservationService _reservationService;

    public CreateReservationHandler(IReservationService reservationService)
    {
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!TryDeserializeRequestBody(request.Body, out CreateReservationRequest? createReservationRequest))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Request body is empty or has invalid format", context.Logger);
        }

        var callerUserId = UserContextHelper.GetAuthenticatedUserId(request);
        if (callerUserId is null)
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized: User ID is missing.", context.Logger);
        }

        var isWaiter = UserContextHelper.IsRequestFromWaiter(request);

        try
        {
            var reservationResponse = await _reservationService.CreateReservationAsync(
                createReservationRequest!,
                callerUserId,
                isWaiter,
                context.Logger);

            return ResponseBuilder.CreateSuccessResponse(reservationResponse, 201);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
    }

    private static bool TryDeserializeRequestBody(string body, out CreateReservationRequest? createReservationRequest)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            createReservationRequest = null;
            return false;
        }

        try
        {
            createReservationRequest = JsonSerializer.Deserialize<CreateReservationRequest>(body);
            return createReservationRequest is not null;
        }
        catch (JsonException)
        {
            createReservationRequest = null;
            return false;
        }
    }
}