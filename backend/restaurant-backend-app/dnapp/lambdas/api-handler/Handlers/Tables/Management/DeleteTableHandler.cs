using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.Tables;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Tables.Management;

public class DeleteTableHandler(ITableService tableService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!RoleHelper.IsWaiter(request)) return ResponseBuilder.CreateErrorResponse(403, "Forbidden", context.Logger);

        if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var tableId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'id'", context.Logger);

        try
        {
            await tableService.DeleteTableAsync(tableId);
            return ResponseBuilder.CreateSuccessResponse(new { message = $"Table '{tableId}' deleted successfully." });
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }
}