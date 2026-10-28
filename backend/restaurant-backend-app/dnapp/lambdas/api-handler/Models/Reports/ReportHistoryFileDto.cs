namespace APIHandler.Models.Reports;

public class ReportHistoryFileDto
{
    public required byte[] FileContent { get; set; }
    
    public required string ContentType { get; set; }
    
    public required string FileName { get; set; }
}