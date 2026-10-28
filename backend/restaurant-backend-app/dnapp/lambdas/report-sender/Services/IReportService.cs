using Amazon.Lambda.Core;
using ReportSender.Entities.Reports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportSender.Services;

public interface IReportService
{
    Task<List<ReportHistoryLocation>> GetLocationsReportAsync(DateOnly startDate, DateOnly endDate, ILambdaLogger logger);

    Task<List<ReportHistoryWaiter>> GetWaitersReportAsync(DateOnly startDate, DateOnly endDate, ILambdaLogger logger);
}