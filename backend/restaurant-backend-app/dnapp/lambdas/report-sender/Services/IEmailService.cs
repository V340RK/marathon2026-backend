using ReportSender.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportSender.Services;

public interface IEmailService
{
    Task SendEmailsWithAttachmentsAsync(IEnumerable<string> recipients, IEnumerable<ReportHistoryFileDto> files, string subject, string body);
}