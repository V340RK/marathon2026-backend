using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.Dishes;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Dishes;

public class DeleteDishHandler(IDishService dishService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var dishId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'id' in path parameters.", context.Logger);

        bool force = false;
        if (request.QueryStringParameters != null && request.QueryStringParameters.TryGetValue("force", out var forceStr))
        {
            bool.TryParse(forceStr, out force);
        }

        try
        {
            await dishService.DeleteDishAsync(dishId, force);
            return ResponseBuilder.CreateSuccessResponse(new { message = $"Dish '{dishId}' deleted successfully." });
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }
}