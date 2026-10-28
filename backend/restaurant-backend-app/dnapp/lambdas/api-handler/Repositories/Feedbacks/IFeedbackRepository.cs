using APIHandler.Models.Feedbacks;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace APIHandler.Repositories.Feedbacks;

public interface IFeedbackRepository
{
    Task<List<Feedback>> GetFeedbackByLocationIdAsync(string locationId);
    Task<List<Feedback>> GetFeedbackByReservationIdAsync(string reservationId);
    Task SaveFeedbackAsync(Feedback feedback);
}
