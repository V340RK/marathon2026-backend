using Amazon.Lambda.Core;
using ReportSender.Entities;
using ReportSender.Services;
using ReportSender.Services.Reports;
using System;
using System.Linq;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ReportSender;

public class Function
{
    private const string Subject = "Weekly Green & Tasty Report";
    private const string Body = "Please find the attached report.";

    private readonly IReportService service = new ReportService();
    private readonly IReportFileService reportFileService = new ReportFileService();
    private readonly IUserService userService = new UserService();
    private readonly IEmailService emailService = new EmailService();

    public async Task FunctionHandler(ILambdaContext context)
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);

        var locationReports = await service.GetLocationsReportAsync(from, to, context.Logger);
        var waiterReports = await service.GetWaitersReportAsync(from, to, context.Logger);

        var locationReportFile = reportFileService.GenerateCsvAsync(locationReports);
        var waiterReportFile = reportFileService.GenerateCsvAsync(waiterReports);

        var admins = await userService.GetUsersByGroupAsync(Roles.Admin.ToString());
        var emails = admins.Select(admin => admin.Attributes
                .FirstOrDefault(a => a.Name == "email")?.Value)
                .Where(email => !string.IsNullOrEmpty(email)).ToList();

        await emailService.SendEmailsWithAttachmentsAsync(emails, [locationReportFile, waiterReportFile], Subject, Body);
    }
}
