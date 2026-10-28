using APIHandler.Entities.Reports;
using APIHandler.Models.Reports;
using APIHandler.Services.Reports.Interfaces;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Document = QuestPDF.Fluent.Document;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace APIHandler.Services.Reports;

public class ReportFileService : IReportFileService
{
    public Task<ReportHistoryFileDto> GenerateCsvAsync(List<ReportHistoryBase> reports)
    {
        var sb = new StringBuilder();

        if (reports.FirstOrDefault() is ReportHistoryWaiter)
        {
            sb.AppendLine(
                "Location,Waiter Name,Email,Total Orders,Working Hours,Average Feedback,Minimal Feedback,Delta Avg Feedback,Delta Orders");

            foreach (var r in reports.Cast<ReportHistoryWaiter>())
            {
                sb.AppendLine(
                    $"{r.LocationName},{r.WaiterName},{r.WaiterEmail},{r.TotalOrders},{r.WorkingHours:F2},{r.AvgServiceFeedback:F2},{r.MinServiceFeedback:F2},{Math.Round(r.DeltaAvgServiceFeedback, 3)}%,{Math.Round(r.DeltaTotalOrders, 3)}%");
            }
        }
        else if (reports.FirstOrDefault() is ReportHistoryLocation)
        {
            sb.AppendLine(
                "Location,Total Orders,Revenue,Average Cuisine Feedback,Minimal Cuisine Feedback,Delta Avg Cuisine Feedback,Delta Revenue");

            foreach (var r in reports.Cast<ReportHistoryLocation>())
            {
                sb.AppendLine(
                    $"{r.LocationName},{r.TotalOrders},{r.Revenue:F2},{r.AvgCuisineFeedback:F2},{r.MinCuisineFeedback:F2},{Math.Round(r.DeltaAvgCuisineFeedback, 3)}%,{Math.Round(r.DeltaRevenue, 3)}%");
            }
        }

        var csvContent = sb.ToString();
        var fileBytes = Encoding.UTF8.GetBytes(csvContent);

        return Task.FromResult(new ReportHistoryFileDto
        {
            FileContent = fileBytes,
            ContentType = "text/csv",
            FileName = $"report_{reports[0].StartDateString}-{reports[0].EndDateString}.csv"
        });
    }

    public ReportHistoryFileDto GenerateExcel(List<ReportHistoryBase> reports)
    {
        var fileName = $"report_{reports[0].StartDateString}-{reports[0].EndDateString}.xls";

        IWorkbook workbook = new HSSFWorkbook(); // for .xls
        ISheet sheet = workbook.CreateSheet("Report");

        if (reports.FirstOrDefault() is ReportHistoryWaiter)
        {
            FillWaiterSheet(sheet, reports.Cast<ReportHistoryWaiter>().ToList());
        }
        else if (reports.FirstOrDefault() is ReportHistoryLocation)
        {
            FillLocationSheet(sheet, reports.Cast<ReportHistoryLocation>().ToList());
        }

        using var ms = new MemoryStream();
        workbook.Write(ms);

        return new ReportHistoryFileDto
        {
            FileContent = ms.ToArray(),
            ContentType = "application/vnd.ms-excel",
            FileName = fileName,
        };
    }

    public ReportHistoryFileDto GeneratePdf(List<ReportHistoryBase> reports)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var stream = new MemoryStream();

        var first = reports.FirstOrDefault();

        var title = first switch
        {
            ReportHistoryWaiter => "Service Report",
            ReportHistoryLocation => "Sales Report",
            _ => "Report"
        };

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text($"{title} ({first?.StartDateString} to {first?.EndDateString})")
                    .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(10).Element(container =>
                {
                    if (first is ReportHistoryWaiter)
                    {
                        var waiterReports = reports.OfType<ReportHistoryWaiter>().ToList();
                        ComposeWaiterItems(container, waiterReports);
                    }
                    else if (first is ReportHistoryLocation)
                    {
                        var locationReports = reports.OfType<ReportHistoryLocation>().ToList();
                        ComposeLocationItems(container, locationReports);
                    }
                });
            });
        }).GeneratePdf(stream);

        return new ReportHistoryFileDto
        {
            FileContent = stream.ToArray(),
            ContentType = "application/pdf",
            FileName = $"report_{reports[0].StartDateString}-{reports[0].EndDateString}.pdf"
        };
    }

    private void ComposeWaiterItems(IContainer container, List<ReportHistoryWaiter> reports)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).PaddingRight(5).Text("Waiter Name");
                header.Cell().Element(CellStyle).Text("Waiter Email");
                header.Cell().Element(CellStyle).Text("Hours");
                header.Cell().Element(CellStyle).Text("Orders");
                header.Cell().Element(CellStyle).Text("Average service feedback");
                header.Cell().Element(CellStyle).Text("Minimal service feedback");
                header.Cell().Element(CellStyle).Text("Delta service feedback");
                header.Cell().Element(CellStyle).Text("Delta orders");

                static IContainer CellStyle(IContainer cell)
                {
                    return cell
                        .DefaultTextStyle(x => x.SemiBold())
                        .PaddingVertical(5)
                        .BorderBottom(1)
                        .BorderColor(Colors.Blue.Darken1)
                        .Background(Colors.Blue.Lighten1);
                }
            });

            foreach (var item in reports)
            {
                table.Cell()
                    .Element(CellStyle)
                    .PaddingLeft(5)
                    .Text(item.WaiterName);

                table.Cell()
                    .Element(CellStyle)
                    .Text(item.WaiterEmail);

                table.Cell()
                    .Element(CellStyle)
                    .Text(item.WorkingHours);

                table.Cell()
                    .Element(CellStyle)
                    .Text(item.TotalOrders);

                table
                    .Cell()
                    .Element(CellStyle)
                    .Text($"{item.AvgServiceFeedback:F2}");

                table
                    .Cell()
                    .Element(CellStyle)
                    .Text($"{item.MinServiceFeedback:F2}");

                table
                    .Cell()
                    .Element(CellStyle)
                    .Text($"{Math.Round(item.DeltaAvgServiceFeedback, 2)}%");

                table
                    .Cell()
                    .Element(CellStyle)
                    .Text($"{Math.Round(item.DeltaTotalOrders, 3)}%");


                static IContainer CellStyle(IContainer container)
                {
                    return container
                        .BorderBottom(1)
                        .BorderColor(Colors.Blue.Lighten2)
                        .PaddingVertical(5);
                }
            }
        });
    }

    private void ComposeLocationItems(IContainer container, List<ReportHistoryLocation> reports)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).PaddingLeft(5).Text("Location Name");
                header.Cell().Element(CellStyle).Text("Total Orders");
                header.Cell().Element(CellStyle).Text("Revenue");
                header.Cell().Element(CellStyle).Text("Avg Cuisine Feedback");
                header.Cell().Element(CellStyle).Text("Min Cuisine Feedback");
                header.Cell().Element(CellStyle).Text("Delta Avg Feedback");
                header.Cell().Element(CellStyle).Text("Delta Revenue");

                static IContainer CellStyle(IContainer cell)
                {
                    return cell
                        .DefaultTextStyle(x => x.SemiBold())
                        .PaddingVertical(5)
                        .BorderBottom(1)
                        .BorderColor(Colors.Blue.Darken1)
                        .Background(Colors.Blue.Lighten1);
                }
            });

            foreach (var item in reports)
            {
                table.Cell().Element(CellStyle).PaddingLeft(5).Text(item.LocationName);
                table.Cell().Element(CellStyle).Text(item.TotalOrders.ToString());
                table.Cell().Element(CellStyle).Text($"{item.Revenue:F2}");
                table.Cell().Element(CellStyle).Text($"{item.AvgCuisineFeedback:F2}");
                table.Cell().Element(CellStyle).Text($"{item.MinCuisineFeedback:F2}");
                table.Cell().Element(CellStyle).Text($"{Math.Round(item.DeltaAvgCuisineFeedback, 2)}%");
                table.Cell().Element(CellStyle).Text($"{Math.Round(item.DeltaRevenue, 3)}%");


                static IContainer CellStyle(IContainer container)
                {
                    return container
                        .BorderBottom(1)
                        .BorderColor(Colors.Blue.Lighten2)
                        .PaddingVertical(5);
                }
            }
        });
    }

    private void FillWaiterSheet(ISheet sheet, List<ReportHistoryWaiter> reports)
    {
        var headers = new[]
        {
            "Waiter ID", "Name", "Email", "Working Hours", "Total Orders",
            "Avg Feedback", "Min Feedback", "Delta Avg", "Delta Orders"
        };

        var headerRow = sheet.CreateRow(0);
        for (int i = 0; i < headers.Length; i++)
            headerRow.CreateCell(i).SetCellValue(headers[i]);

        for (int i = 0; i < reports.Count; i++)
        {
            var r = reports[i];
            var row = sheet.CreateRow(i + 1);

            row.CreateCell(0).SetCellValue(r.WaiterId ?? "");
            row.CreateCell(1).SetCellValue(r.WaiterName ?? "");
            row.CreateCell(2).SetCellValue(r.WaiterEmail ?? "");
            row.CreateCell(3).SetCellValue(r.WorkingHours);
            row.CreateCell(4).SetCellValue(r.TotalOrders);
            row.CreateCell(5).SetCellValue(Math.Round(r.AvgServiceFeedback, 2));
            row.CreateCell(6).SetCellValue(Math.Round(r.MinServiceFeedback, 2));
            row.CreateCell(7).SetCellValue($"{Math.Round(r.DeltaAvgServiceFeedback, 2)}%");
            row.CreateCell(8).SetCellValue($"{Math.Round(r.DeltaTotalOrders, 3)}%");
        }
    }

    private void FillLocationSheet(ISheet sheet, List<ReportHistoryLocation> reports)
    {
        var headers = new[]
        {
            "Location Name", "Total Orders", "Revenue",
            "Avg Cuisine Feedback", "Min Cuisine Feedback",
            "Delta Avg Cuisine Feedback", "Delta Revenue"
        };

        var headerRow = sheet.CreateRow(0);
        for (int i = 0; i < headers.Length; i++)
            headerRow.CreateCell(i).SetCellValue(headers[i]);

        for (int i = 0; i < reports.Count; i++)
        {
            var r = reports[i];
            var row = sheet.CreateRow(i + 1);

            row.CreateCell(0).SetCellValue(r.LocationName ?? "");
            row.CreateCell(1).SetCellValue(r.TotalOrders);
            row.CreateCell(2).SetCellValue((double)r.Revenue);
            row.CreateCell(3).SetCellValue(Math.Round(r.AvgCuisineFeedback, 2));
            row.CreateCell(4).SetCellValue(Math.Round(r.MinCuisineFeedback, 2));
            row.CreateCell(5).SetCellValue($"{Math.Round(r.DeltaAvgCuisineFeedback, 2)}%");
            row.CreateCell(6).SetCellValue($"{Math.Round((double)r.DeltaRevenue, 3)}%");
        }
    }
}