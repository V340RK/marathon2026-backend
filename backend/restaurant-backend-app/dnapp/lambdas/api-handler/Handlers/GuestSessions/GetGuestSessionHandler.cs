using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class GetGuestSessionHandler(IGuestSessionService guestSessionService, IGuestAuthorizationService guestAuthorizationService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("guestSessionId", out string? guestSessionId))
            return ResponseBuilder.CreateErrorResponse(404, "Guest session not found", context.Logger);

        if (await guestAuthorizationService.ValidateAsync(request, context, guestSessionId) is APIGatewayProxyResponse errorResponse)
            return errorResponse;

        try
        {
            GetGuestSessionResponse result = await guestSessionService.GetGuestSessionInfoAsync(guestSessionId);
            return ResponseBuilder.CreateJsonResponse(200, result);
        }
        catch (GuestSessionNotFoundException exception)
        {
            return ResponseBuilder.CreateJsonResponse(404, exception.Message);
        }
        catch (ValidationException exception)
        {
            return ResponseBuilder.CreateErrorCodeResponse(exception);
        }
    }
}
