using APIHandler.Models.Feedbacks;

namespace APIHandler.Mappers;

public static class FeedbackMapper
{
    public static FeedbackResponseItem ToResponseItem(this Feedback feedback)
    {
        return new FeedbackResponseItem
        {
            ReservationId = feedback.ReservationId,
            LocationId = feedback.LocationId,
            UserId = feedback.UserId,
            Comment = feedback.Comment,
            Rating = feedback.Rating,
            CreatedAt = feedback.CreatedAt,
            Type = feedback.Type
        };
    }

    public static Feedback ToModel(this FeedbackResponseItem dto)
    {
        return new Feedback
        {
            ReservationId = dto.ReservationId,
            LocationId = dto.LocationId,
            UserId = dto.UserId,
            Comment = dto.Comment,
            Rating = dto.Rating,
            CreatedAt = dto.CreatedAt,
            Type = dto.Type
        };
    }
}