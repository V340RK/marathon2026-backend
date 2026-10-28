using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Services.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Tables;

public class GetAvailableTablesHandler(ITableService tableService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();

        queryParams.TryGetValue("locationId", out string? locationId);
        queryParams.TryGetValue("capacity", out string? capacityStr);
        queryParams.TryGetValue("startTime", out string? startTimeStr);

        int.TryParse(capacityStr, out int capacity);
        DateTime.TryParse(startTimeStr, out DateTime startTime);

        if (startTime != default && startTime < DateTime.UtcNow)
        {
            return ResponseBuilder.CreateJsonResponse(400, new { message = "Cannot query timeslots in the past." });
        }

        var availableTables = await tableService.GetAvailableTablesAsync(locationId, capacity, startTime);
        return ResponseBuilder.CreateJsonResponse(200, availableTables);
    }
}