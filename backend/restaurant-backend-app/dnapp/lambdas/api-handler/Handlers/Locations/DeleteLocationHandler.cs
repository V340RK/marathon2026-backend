using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.Locations;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Locations;

public class DeleteLocationHandler : IApiRequestHandler
{
    private readonly ILocationService _locationService;

    public DeleteLocationHandler(ILocationService locationService)
    {
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var locationId) || string.IsNullOrWhiteSpace(locationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'id' in path parameters.", context.Logger);
        }

        bool force = false;
        if (request.QueryStringParameters != null &&
            request.QueryStringParameters.TryGetValue("force", out var forceStr))
        {
            bool.TryParse(forceStr, out force);
        }

        try
        {
            await _locationService.DeleteLocationAsync(locationId, force);
            return ResponseBuilder.CreateSuccessResponse(new { message = $"Location '{locationId}' deleted successfully." });
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error deleting location {locationId}: {ex.Message}\n{ex.StackTrace}");
            return ResponseBuilder.CreateErrorResponse(500, "An internal error occurred while deleting the location.", context.Logger);
        }
    }
}