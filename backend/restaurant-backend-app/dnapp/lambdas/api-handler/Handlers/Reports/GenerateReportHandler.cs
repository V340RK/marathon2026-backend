using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants.Reports;
using APIHandler.Entities.Reports;
using APIHandler.Helpers;
using APIHandler.Services.Locations;
using APIHandler.Services.Reports.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Reports;

public class GenerateReportHandler : IApiRequestHandler
{
    private readonly IReportService _reportService;
    private readonly ILocationService _locationService;

    public GenerateReportHandler(IReportService reportService, ILocationService locationService)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var (success, parsedParams, errorResponse) = TryParseQueryParameters(request.QueryStringParameters);

        if (!success)
        {
            return errorResponse!;
        }

        // check if location exists
        var location = await _locationService.GetLocationByIdAsync(parsedParams!.LocationId);
        if (location == null)
        {
            return ResponseBuilder.CreateErrorResponse(404, $"Location with ID {parsedParams.LocationId} not found.", context.Logger);
        }

        var userId = UserContextHelper.GetAuthenticatedUserId(request);
        context.Logger.Log($"{userId} {JsonSerializer.Serialize(parsedParams)}");
        if (userId is null)
        {
            return ResponseBuilder.CreateErrorResponse(401, $"User are not authorized!", context.Logger);
        }

        var reportsHistoryLines =
            await _reportService.QueryReportsHistoryAsync(
                userId,
                parsedParams.ReportType,
                parsedParams.LocationId,
                parsedParams.StartDate,
                parsedParams.EndDate);

        context.Logger.LogInformation($"LINES {JsonSerializer.Serialize(reportsHistoryLines)}");

        if (reportsHistoryLines.Count > 0)
        {
            switch (parsedParams.ReportType)
            {
                case ReportType.Service:
                    var waiterReports = reportsHistoryLines.OfType<ReportHistoryWaiter>().ToList();
                    if (waiterReports.Count > 0)
                        return ResponseBuilder.CreateSuccessResponse(waiterReports);
                    break;

                case ReportType.Sales:
                    var locationReports = reportsHistoryLines.OfType<ReportHistoryLocation>().ToList();
                    if (locationReports.Count > 0)
                        return ResponseBuilder.CreateSuccessResponse(locationReports);
                    break;
            }
        }

        // get reports (aggregates)
        var aggregates = await _reportService.GetAggregatesForPeriodAsync(parsedParams.StartDate, parsedParams.EndDate, parsedParams.LocationId);
        context.Logger.LogInformation($"AGGREGATES {JsonSerializer.Serialize(aggregates)}");

        if (aggregates is null || aggregates.Count == 0)
        {
            return ResponseBuilder.CreatePlainTextResponse(200, "Reports were not found");
        }

        // generate and save to ReportsHistory
        if (parsedParams.ReportType == ReportType.Service)
        {
            var waiterReports = await _reportService.GenerateWaiterReportsAsync
                (aggregates, parsedParams.StartDate, parsedParams.EndDate, userId, context.Logger);
            // context.Logger.LogInformation($"SERVICE {JsonSerializer.Serialize(waiterReports)}");

            await _reportService.SaveReportsHistoryAsync(waiterReports);
            return ResponseBuilder.CreateSuccessResponse(waiterReports);
        }
        else if (parsedParams.ReportType == ReportType.Sales)
        {
            var locationReports = await _reportService.GenerateLocationReportsAsync
                (aggregates, parsedParams.StartDate, parsedParams.EndDate, userId, context.Logger);
            // context.Logger.LogInformation($"LOCATION {JsonSerializer.Serialize(locationReports)}");

            await _reportService.SaveReportsHistoryAsync(locationReports);
            return ResponseBuilder.CreateSuccessResponse(locationReports);
        }

        return ResponseBuilder.CreatePlainTextResponse(200, "Reports were not found");
    }

    private (bool Success, ParsedParams? Params, APIGatewayProxyResponse? ErrorResponse) TryParseQueryParameters(
        IDictionary<string, string>? queryParams)
    {
        if (queryParams == null)
            queryParams = new Dictionary<string, string>();

        queryParams.TryGetValue("type", out var reportTypeStr);
        queryParams.TryGetValue("startDate", out var startDateStr);
        queryParams.TryGetValue("endDate", out var endDateStr);
        queryParams.TryGetValue("locationId", out var locationId);

        if (string.IsNullOrEmpty(locationId))
        {
            return (false, null,
                ResponseBuilder.CreatePlainTextResponse(400, "Missing required parameter: locationId"));
        }

        if (!DateOnly.TryParse(startDateStr, out var startDate))
        {
            return (false, null, ResponseBuilder.CreatePlainTextResponse(400, "Invalid or missing startDate"));
        }

        if (!DateOnly.TryParse(endDateStr, out var endDate))
        {
            return (false, null, ResponseBuilder.CreatePlainTextResponse(400, "Invalid or missing endDate"));
        }

        if (startDate > endDate)
        {
            return (false, null,
                ResponseBuilder.CreatePlainTextResponse(400, "startDate must be before or equal to endDate"));
        }

        var reportType = ReportType.Service;
        if (!string.IsNullOrEmpty(reportTypeStr) &&
            !Enum.TryParse<ReportType>(reportTypeStr, ignoreCase: true, out reportType))
        {
            return (false, null, ResponseBuilder.CreatePlainTextResponse(400, $"Invalid reportType: {reportTypeStr}"));
        }

        return (true, new ParsedParams
        {
            ReportType = reportType,
            StartDate = startDate,
            EndDate = endDate,
            LocationId = locationId
        }, null);
    }

    private record ParsedParams
    {
        public ReportType ReportType { get; init; }

        public DateOnly StartDate { get; init; }

        public DateOnly EndDate { get; init; }
        
        public required string LocationId { get; init; }
    }
}