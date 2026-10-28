using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Models.Reservations;
using APIHandler.Services.Reservations;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using APIHandler.Exceptions;

namespace APIHandler.Handlers.Reservations;

public class UpdateReservationTableHandler : IApiRequestHandler
{
    private readonly IReservationService _reservationService;

    public UpdateReservationTableHandler(IReservationService reservationService)
    {
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var callerId = UserContextHelper.GetAuthenticatedUserId(request);
        if (callerId is null)
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);
        }

        var isWaiter = UserContextHelper.IsRequestFromWaiter(request);
        if (!isWaiter)
        {
            return ResponseBuilder.CreateErrorResponse(403, "Only waiters can update reservation tables.", context.Logger);
        }

        if (!request.PathParameters.TryGetValue("reservationId", out var reservationId) || DataValidationHelper.IsMissing(reservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing reservation ID.", context.Logger);
        }

        if (DataValidationHelper.IsMissing(request.Body))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Request body is missing.", context.Logger);
        }

        UpdateReservationTableRequest updateRequest;
        try
        {
            updateRequest = JsonSerializer.Deserialize<UpdateReservationTableRequest>(request.Body)
                ?? throw new InvalidOperationException("Invalid request format.");
        }
        catch (JsonException)
        {
            return ResponseBuilder.CreateErrorResponse(400, "Malformed JSON.", context.Logger);
        }

        if (DataValidationHelper.IsMissing(updateRequest.TableId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "TableId must be provided.", context.Logger);
        }

        try
        {
            var result = await _reservationService.UpdateReservationTableAsync(callerId, reservationId, updateRequest.TableId, context.Logger);
            return ResponseBuilder.CreateSuccessResponse(result, 200);
        }
        catch (ReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
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
}
