using APIHandler.Entities.Reports;
using APIHandler.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Reports.Interfaces;

public interface IReportFileService
{
    Task<ReportHistoryFileDto> GenerateCsvAsync(List<ReportHistoryBase> reports);

    ReportHistoryFileDto GenerateExcel(List<ReportHistoryBase> reports);

    ReportHistoryFileDto GeneratePdf(List<ReportHistoryBase> reports);
}