using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class CreateGuestSessionHandler(IGuestSessionService guestSessionService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!RequestBodyHelper.TryParseAndValidate(request, context, out CreateGuestSessionRequest? requestBody, out APIGatewayProxyResponse? errorResponse))
            return errorResponse;

        try
        {
            var result = await guestSessionService.CreateGuestSessionAsync(requestBody);
            return ResponseBuilder.CreateSuccessResponse(result);
        }
        catch (ValidationException exception)
        {
            return ResponseBuilder.CreateErrorCodeResponse(exception);
        }
    }
}
