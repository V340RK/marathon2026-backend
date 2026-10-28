using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Users;
using APIHandler.Services.Locations;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Locations;

public class RemoveDishFromLocationHandler(ILocationService locationService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.RequestContext?.Authorizer == null ||
            !request.RequestContext.Authorizer.Claims.TryGetValue("cognito:groups", out var groups) ||
            string.IsNullOrEmpty(groups) ||
            !groups.Contains(Roles.Waiter.ToString()))
        {
            return ResponseBuilder.CreateErrorResponse(403, "Forbidden: Only waiters can perform this action.", context.Logger);
        }

        if (request.PathParameters == null ||
            !request.PathParameters.TryGetValue("locationId", out var locationId) ||
            !request.PathParameters.TryGetValue("dishId", out var dishId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'locationId' or 'dishId' in path parameters.", context.Logger);
        }

        try
        {
            await locationService.RemoveDishFromLocationAsync(locationId, dishId);
            return ResponseBuilder.CreateSuccessResponse(new { message = $"Dish '{dishId}' successfully removed from location '{locationId}'." });
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }
}