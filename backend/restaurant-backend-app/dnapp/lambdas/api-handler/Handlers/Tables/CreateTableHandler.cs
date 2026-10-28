using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Models.Tables;
using APIHandler.Services.Tables;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Tables;

public class CreateTableHandler(ITableService tableService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ResponseBuilder.CreatePlainTextResponse(400, "Request body is empty");
        }

        CreateTableRequest? createTableRequest;
        try
        {
            createTableRequest = JsonSerializer.Deserialize<CreateTableRequest>(request.Body);
        }
        catch (JsonException)
        {
            return ResponseBuilder.CreatePlainTextResponse(400, "Invalid JSON format");
        }

        if (createTableRequest is null
            || string.IsNullOrWhiteSpace(createTableRequest.LocationId)
            || createTableRequest.Capacity <= 0
            || createTableRequest.TableNumber <= 0)
        {
            return ResponseBuilder.CreatePlainTextResponse(400, "Invalid payload");
        }

        var table = await tableService.CreateTableAsync(createTableRequest);
        return ResponseBuilder.CreateJsonResponse(201, table);
    }
}