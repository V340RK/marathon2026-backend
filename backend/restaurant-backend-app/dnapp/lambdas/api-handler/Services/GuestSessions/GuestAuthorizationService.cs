using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public class GuestAuthorizationService(IGuestSessionService guestSessionService) : IGuestAuthorizationService
{
    public async ValueTask<APIGatewayProxyResponse?> ValidateAsync(APIGatewayProxyRequest request, ILambdaContext context, string guestSessionid)
    {
        if (!request.Headers.TryGetValue("Authorization", out string? authorizationValue))
            return CreateErrorResponse(context);

        const string BearerString = "Bearer ";
        if (!authorizationValue.StartsWith(BearerString) || authorizationValue.Length <= BearerString.Length)
            return CreateErrorResponse(context);

        string token = authorizationValue[BearerString.Length..];
        bool authorized = await guestSessionService.IsAuthorizedAsync(token, guestSessionid);

        return !authorized ? CreateErrorResponse(context) : null;
    }

    private static APIGatewayProxyResponse CreateErrorResponse(ILambdaContext context)
        => ResponseBuilder.CreateErrorResponse(401, "Invalid guest session token", context.Logger);
}
