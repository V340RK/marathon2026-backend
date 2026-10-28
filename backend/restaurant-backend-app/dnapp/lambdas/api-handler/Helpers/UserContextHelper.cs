using Amazon.Lambda.APIGatewayEvents;
using APIHandler.Models.Users;
using System.Linq;

namespace APIHandler.Helpers;

public static class UserContextHelper
{
    public static string? GetAuthenticatedUserId(APIGatewayProxyRequest request)
    {
        if (request.RequestContext.Authorizer?.Claims is not null &&
            request.RequestContext.Authorizer.Claims.TryGetValue("sub", out var userId) &&
            !string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }
        return null;
    }

    public static bool IsRequestFromWaiter(APIGatewayProxyRequest request)
    {
        return request.RequestContext.Authorizer?.Claims is not null &&
            request.RequestContext.Authorizer.Claims.TryGetValue("cognito:groups", out var groups) &&
            groups.Split(',')
            .Select(x => x.Trim())
            .Contains(Roles.Waiter.ToString());
    }
}
