using ReportSender.Entities.Reports;
using ReportSender.Models.Reports;
using System.Collections.Generic;

namespace ReportSender.Services;

public interface IReportFileService
{
    ReportHistoryFileDto GenerateCsvAsync(IEnumerable<ReportHistoryBase> reports);
}