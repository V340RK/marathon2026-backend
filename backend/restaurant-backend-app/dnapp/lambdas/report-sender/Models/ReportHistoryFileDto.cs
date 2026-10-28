namespace ReportSender.Models.Reports;

public class ReportHistoryFileDto
{
    public byte[] FileContent { get; set; }

    public string ContentType { get; set; }

    public string FileName { get; set; }
}