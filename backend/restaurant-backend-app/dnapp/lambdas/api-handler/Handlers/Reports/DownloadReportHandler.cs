using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants.Reports;
using APIHandler.Helpers;
using APIHandler.Services.Reports.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Reports;

public class DownloadReportHandler : IApiRequestHandler
{
    private readonly IReportFileService _reportFileService;
    private readonly IReportService _reportService;

    public DownloadReportHandler(IReportFileService reportFileService, IReportService reportService)
    {
        _reportFileService = reportFileService ?? throw new ArgumentNullException(nameof(reportFileService));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();
        var (success, parsed, error) = TryParseReportQueryParams(queryParams, context.Logger);

        if (!success)
            return error!;

        context.Logger.LogLine
            ($"[Report Request] format={parsed!.Format}, type={parsed.ReportType}, startDate={parsed.StartDate}, endDate={parsed.EndDate}, locationId={parsed.LocationId}");
        
        var userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (userId is null)
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized: User ID is missing.", context.Logger);
        }

        var reportData = await _reportService.QueryReportsHistoryAsync(userId, parsed.ReportType, parsed.LocationId, parsed.StartDate, parsed.EndDate);

        var fileDto = parsed.Format switch
        {
            FileType.Excel => _reportFileService.GenerateExcel(reportData),
            FileType.Pdf => _reportFileService.GeneratePdf(reportData),
            FileType.Csv => await _reportFileService.GenerateCsvAsync(reportData),
            _ => await _reportFileService.GenerateCsvAsync(reportData),
        };

        return ResponseBuilder.CreateFileResponse(
            fileDto.FileContent,
            fileDto.FileName,
            fileDto.ContentType);
    }

    private (bool Success, ReportQueryParams? Params, APIGatewayProxyResponse? ErrorResponse) TryParseReportQueryParams(
        IDictionary<string, string> queryParams,
        ILambdaLogger logger)
    {
        queryParams.TryGetValue("format", out var format);
        queryParams.TryGetValue("type", out var reportTypeStr);
        queryParams.TryGetValue("startDate", out var startDateStr);
        queryParams.TryGetValue("endDate", out var endDateStr);
        queryParams.TryGetValue("locationId", out var locationId);

        logger.LogLine($"[Report Request] format={format}, type={reportTypeStr}, startDate={startDateStr}, endDate={endDateStr}, locationId={locationId}");

        if (string.IsNullOrWhiteSpace(locationId))
            return (false, null, ResponseBuilder.CreateErrorResponse(400, "Missing locationId", logger));

        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-7);

        if (!string.IsNullOrEmpty(startDateStr) && DateOnly.TryParse(startDateStr, out var parsedStart))
            start = parsedStart;

        if (!string.IsNullOrEmpty(endDateStr) && DateOnly.TryParse(endDateStr, out var parsedEnd))
            end = parsedEnd;

        if (start > end)
            return (false, null, ResponseBuilder.CreateErrorResponse(400, "startDate must be before endDate", logger));

        var reportType = ReportType.Service;
        if (!string.IsNullOrEmpty(reportTypeStr) &&
            !Enum.TryParse<ReportType>(reportTypeStr, true, out reportType))
            return (false, null, ResponseBuilder.CreateErrorResponse(400, $"Invalid report type: {reportTypeStr}", logger));

        return (true, new ReportQueryParams
        {
            Format = format ?? FileType.Csv,
            ReportType = reportType,
            StartDate = start,
            EndDate = end,
            LocationId = locationId
        }, null);
    }

    private record ReportQueryParams
    {
        public string Format { get; init; } = null!;
        public ReportType ReportType { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public string LocationId { get; init; } = null!;
    }
}