using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class SubmitGuestCartHandler(IGuestSessionService service, IGuestAuthorizationService guestAuthorizationService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("guestSessionId", out string? guestSessionId))
        {
            return ResponseBuilder.CreateErrorResponse(404, "Guest session is required.", context.Logger);
        }

        if (await guestAuthorizationService.ValidateAsync(request, context, guestSessionId) is APIGatewayProxyResponse errorResponse)
            return errorResponse;

        try
        {
            await service.SubmitCartAsync(guestSessionId);
        }
        catch (GroupReservationExpiredException exception)
        {
            return ResponseBuilder.CreateErrorResponse(409, exception.Message, context.Logger);
        }
        catch (GroupReservationSubmittedException exception)
        {
            return ResponseBuilder.CreateErrorResponse(409, exception.Message, context.Logger);
        }
        catch (GuestSessionSubmittedException exception)
        {
            return ResponseBuilder.CreateErrorResponse(409, exception.Message, context.Logger);
        }
        catch(ValidationException exception)
        {
            return ResponseBuilder.CreateErrorCodeResponse(exception);
        }

        return ResponseBuilder.CreateJsonResponse(200, new SuccessResponse("Cart submitted successfully."));
    }
}
