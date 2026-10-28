using ReportSender.Entities.Reports;
using ReportSender.Models.Reports;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportSender.Services.Reports;

public class ReportFileService : IReportFileService
{
    public ReportHistoryFileDto GenerateCsvAsync(IEnumerable<ReportHistoryBase> reports)
    {
        var sb = new StringBuilder();

        if (reports.FirstOrDefault() is ReportHistoryWaiter)
        {
            sb.AppendLine("Location,Waiter,Email,TotalOrders,WorkingHours,AvgServiceFeedback,MinServiceFeedback,DeltaAvgServiceFeedback,DeltaTotalOrders");
            foreach (var r in reports.Cast<ReportHistoryWaiter>())
            {
                sb.AppendLine($"{r.LocationName},{r.WaiterName},{r.WaiterEmail},{r.TotalOrders},{r.WorkingHours},{r.AvgServiceFeedback},{r.MinServiceFeedback},{r.DeltaAvgServiceFeedback},{r.DeltaTotalOrders}");
            }
        }
        else if (reports.FirstOrDefault() is ReportHistoryLocation)
        {
            sb.AppendLine("Location,TotalOrders,Revenue,AvgCuisineFeedback,MinCuisineFeedback,DeltaAvgCuisineFeedback,DeltaRevenue");
            foreach (var r in reports.Cast<ReportHistoryLocation>())
            {
                sb.AppendLine($"{r.LocationName},{r.TotalOrders},{r.Revenue},{r.AvgCuisineFeedback},{r.MinCuisineFeedback},{r.DeltaAvgCuisineFeedback},{r.DeltaRevenue}");
            }
        }

        var csvContent = sb.ToString();

        var fileBytes = Encoding.UTF8.GetBytes(csvContent);

        return new ReportHistoryFileDto
        {
            FileContent = fileBytes,
            ContentType = "text/csv",
            FileName = reports.Any() 
                ? $"report_{reports.First().StartDateString}-{reports.First().EndDateString}.csv"
                : $"report.csv"
        };
    }
}