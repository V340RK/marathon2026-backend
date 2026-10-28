using APIHandler.Models.Feedbacks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Feedbacks;

public interface IFeedbackService
{
    /// <summary>
    /// Creates feedback for a reservation.
    /// </summary>
    /// <param name="request">The request containing feedback details.</param>
    /// <returns>The created feedback.</returns>
    Task<FeedbackResponseItem> CreateFeedbackAsync(CreateFeedbackRequest request);

    /// <summary>
    /// Retrieves feedback for a specific location.
    /// </summary>
    /// <param name="locationId">The ID of the location.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="sortOrder">The sort order for the feedback.</param>
    /// <param name="type">The type of feedback to retrieve.</param>
    /// <returns>A list of feedback for the specified location.</returns>
    Task<FeedbackResponse> GetFeedbackForLocationAsync(string locationId, int page, FeedbackType type, SortingOrder sortOrder);


    /// <summary>
    /// Retrieves feedback for a specific reservation.
    /// /// </summary>
    /// <param name="reservationId">The ID of the reservation.</param>
    /// <returns>The feedback associated with the reservation.</returns>
    Task<List<FeedbackResponseItem>> GetFeedbackForReservationAsync(string reservationId);

    /// <summary>
    /// Updates existing feedback for a reservation.
    /// /// </summary>
    /// <param name="request">The request containing updated feedback details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateFeedbackAsync(CreateFeedbackRequest request);
}
