using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GroupReservation;
using APIHandler.Services.GroupReservations;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GroupReservations;

public class GetInviteTokenHandler(IGroupReservationService service) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("reservationId", out string? reservationId))
            return ResponseBuilder.CreateErrorResponse(404, "Reservation was not found", context.Logger);

        try
        {
            GetGroupInviteTokenResponse result = await service.GetGroupReservationInviteTokenAsync(reservationId);
            return ResponseBuilder.CreateJsonResponse(200, result);
        }
        catch (ReservationNotFoundException exception)
        {
            return ResponseBuilder.CreateErrorResponse(404, exception.Message, context.Logger, exception);
        }
        catch(ValidationException exception)
        {
            return ResponseBuilder.CreateErrorCodeResponse(exception);
        }
    }   
}
