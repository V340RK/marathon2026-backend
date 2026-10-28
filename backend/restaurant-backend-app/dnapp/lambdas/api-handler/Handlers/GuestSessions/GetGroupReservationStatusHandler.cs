using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.GuestSessions;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class GetGroupReservationStatusHandler(IGuestSessionService guestSessionService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("groupReservationId", out string? groupReservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing groupReservationId in path parameters.", context.Logger);
        }

        try
        {
            var status = await guestSessionService.GetGroupReservationStatusAsync(groupReservationId);
            return ResponseBuilder.CreateSuccessResponse(status);
        }
        catch (GroupReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
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
}