using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using MimeKit;
using ReportSender.Models.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReportSender.Services;

public class EmailService : IEmailService
{
    private static readonly string From = Environment.GetEnvironmentVariable("EmailFrom")
        ?? throw new InvalidOperationException("EmailFrom environment variable is not set.");

    public async Task SendEmailsWithAttachmentsAsync(
        IEnumerable<string> recipients, IEnumerable<ReportHistoryFileDto> files, string subject, string body)
    {
        var message = CreateEmailMessage(recipients, subject, body, files);

        await SendRawEmailAsync(message);
    }

    private static MimeMessage CreateEmailMessage(
        IEnumerable<string> recipients, string subject, string body, IEnumerable<ReportHistoryFileDto> files)
    {
        var message = new MimeMessage();

        message.From.Add(MailboxAddress.Parse(From));
        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = body };

        AddAttachments(builder, files);

        message.Body = builder.ToMessageBody();

        return message;
    }

    private static void AddAttachments(BodyBuilder builder, IEnumerable<ReportHistoryFileDto> files)
    {
        foreach (var file in files)
        {
            var contentType = ParseContentType(file.ContentType);
            builder.Attachments.Add(file.FileName, file.FileContent, contentType);
        }
    }

    private static ContentType ParseContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return new ContentType("application", "octet-stream");
        }

        var parts = contentType.Split('/');
        return parts.Length == 2 ? new ContentType(parts[0], parts[1]) :
            new ContentType("application", "octet-stream");
    }

    private static async Task SendRawEmailAsync(MimeMessage message)
    {
        using var client = new AmazonSimpleEmailServiceClient();
        using var stream = new MemoryStream();
        await message.WriteToAsync(stream);
        stream.Position = 0;

        var sendRequest = new SendRawEmailRequest
        {
            RawMessage = new RawMessage(stream)
        };

        await client.SendRawEmailAsync(sendRequest);
    }
}