using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Services.Auth;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Users;

public class GetUserNameByIdHandler : IApiRequestHandler
{
    private readonly IUserService _userService;

    public GetUserNameByIdHandler(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.PathParameters.TryGetValue("id", out var userId) || string.IsNullOrEmpty(userId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "User ID is required.", context.Logger);
        }

        var userName = await _userService.GetUserName(userId) ?? "Anonymous User";

        return ResponseBuilder.CreateSuccessResponse(new { name = userName });
    }
}