namespace APIHandler.Models.Feedbacks;

public class FeedbackQueryParameters
{
    public required string LocationId { get; set; }

    public required FeedbackType Type { get; set; }

    public required SortingOrder SortOrder { get; set; }

    public required int Page { get; set; }
}