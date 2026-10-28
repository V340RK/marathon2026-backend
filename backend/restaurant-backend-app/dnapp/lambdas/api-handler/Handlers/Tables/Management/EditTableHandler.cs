using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Tables;
using APIHandler.Services.Tables;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Tables.Management;

public class EditTableHandler(ITableService tableService) : IApiRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!RoleHelper.IsWaiter(request)) return ResponseBuilder.CreateErrorResponse(403, "Forbidden", context.Logger);

        if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var tableId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'id'", context.Logger);

        if (string.IsNullOrWhiteSpace(request.Body))
            return ResponseBuilder.CreateErrorResponse(400, "Request body is empty", context.Logger);

        var updateRequest = JsonSerializer.Deserialize<UpdateTableRequest>(request.Body, JsonOptions);
        if (updateRequest == null) return ResponseBuilder.CreateErrorResponse(400, "Invalid payload", context.Logger);

        try
        {
            var updatedTable = await tableService.UpdateTableAsync(tableId, updateRequest);
            return ResponseBuilder.CreateSuccessResponse(updatedTable);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }
}