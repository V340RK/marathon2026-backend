using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.GuestSessions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class GetGuestSessionMenuHandler : IApiRequestHandler
{
    private readonly IGuestSessionService _guestSessionService;
    private readonly IGuestAuthorizationService _guestAuthorizationService;

    public GetGuestSessionMenuHandler(IGuestSessionService guestSessionService, IGuestAuthorizationService guestAuthorizationService)
    {
        _guestSessionService = guestSessionService ?? throw new ArgumentNullException(nameof(guestSessionService));
        _guestAuthorizationService = guestAuthorizationService ?? throw new ArgumentNullException(nameof(guestAuthorizationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("guestSessionId", out var guestSessionId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'guestSessionId' in path parameters.", context.Logger);
        }

        if (await _guestAuthorizationService.ValidateAsync(request, context, guestSessionId) is APIGatewayProxyResponse error)
            return error;

        try
        {
            var menu = await _guestSessionService.GetGuestMenuAsync(guestSessionId);
            return ResponseBuilder.CreateJsonResponse(200, menu);
        }
        catch (GroupReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorCodeResponse(ex);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
    }
}