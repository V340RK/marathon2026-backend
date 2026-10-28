using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Services.Reports.Interfaces;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Reports;

public class GetUserReportsHandler : IApiRequestHandler
{
    private readonly IReportService _reportService;

    public GetUserReportsHandler(IReportService reportService)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (userId is null)
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized: User ID is missing.", context.Logger);
        }

        var reportData = await _reportService.GetUserReportsListAsync(userId);
        // context.Logger.LogInformation($"{JsonSerializer.Serialize(reportData)}");

        return ResponseBuilder.CreateSuccessResponse(reportData);
    }
}