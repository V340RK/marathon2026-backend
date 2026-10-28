using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Helpers;
using APIHandler.Services.Tables;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Tables.Management;

public class GetLocationTablesHandler(ITableService tableService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!RoleHelper.IsWaiter(request)) return ResponseBuilder.CreateErrorResponse(403, "Forbidden", context.Logger);

        if (request.PathParameters == null || !request.PathParameters.TryGetValue("locationId", out var locationId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'locationId'", context.Logger);

        var tables = await tableService.GetAllTablesForLocationAsync(locationId);
        return ResponseBuilder.CreateSuccessResponse(tables);
    }
}