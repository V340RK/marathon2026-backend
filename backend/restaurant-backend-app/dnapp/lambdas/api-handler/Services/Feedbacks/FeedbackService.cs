using APIHandler.Exceptions;
using APIHandler.Mappers;
using APIHandler.Models.Feedbacks;
using APIHandler.Repositories.Feedbacks;
using APIHandler.Services.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.Feedbacks;

public class FeedbackService : IFeedbackService
{
    private const int DefaultItemsPerPage = 4;

    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IReservationService _reservationService;

    public FeedbackService(IFeedbackRepository feedbackRepository, IReservationService reservationService)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    public async Task<FeedbackResponseItem> CreateFeedbackAsync(CreateFeedbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReservationId))
        {
            throw new ValidationException("Reservation ID cannot be null or empty.");
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new ValidationException($"Rating ({request.Rating}) must be between 1 and 5.");
        }

        var feedback = new Feedback
        {
            ReservationId = request.ReservationId,
            LocationId = await _reservationService.GetLocationIdForReservation(request.ReservationId),
            UserId = request.UserId,
            Comment = request.Comment,
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow,
            Type = request.Type
        };

        await _feedbackRepository.SaveFeedbackAsync(feedback);
        return feedback.ToResponseItem();
    }

    public async Task<FeedbackResponse> GetFeedbackForLocationAsync(string locationId, int page, FeedbackType type, SortingOrder sortOrder)
    {
        var query = await _feedbackRepository.GetFeedbackByLocationIdAsync(locationId);
        if (query is null || query.Count == 0)
        {
            return new FeedbackResponse
            {
                Items = [],
                TotalPages = 0,
                CurrentPage = page
            };
        }

        var filteredFeedback = query
            .Where(f => f.Type == type)
            .OrderBy(f => GetFeedbackSortValue(f, sortOrder))
            .Skip((page - 1) * DefaultItemsPerPage)
            .Take(DefaultItemsPerPage)
            .ToList();
        var totalPages = (int)Math.Ceiling((double)query.Count / DefaultItemsPerPage);
        return new FeedbackResponse
        {
            Items = [.. filteredFeedback.Select(f => f.ToResponseItem())],
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task UpdateFeedbackAsync(CreateFeedbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReservationId))
        {
            throw new ValidationException("Reservation ID cannot be null or empty.");
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new ValidationException($"Rating ({request.Rating}) must be between 1 and 5.");
        }

        var existingFeedback = await GetFeedbackForReservationAsync(request.ReservationId);
        if (existingFeedback.Count == 0)
        {
            throw new ValidationException($"No feedback found for reservation '{request.ReservationId}'.");
        }

        var feedbackToUpdate = existingFeedback.SingleOrDefault(f => f.Type == request.Type);
        if (feedbackToUpdate is null)
        {
            throw new ValidationException($"No feedback of type '{request.Type}' found for reservation '{request.ReservationId}'.");
        }

        var modelToUpdate = feedbackToUpdate.ToModel();
        modelToUpdate.Comment = request.Comment;
        modelToUpdate.Rating = request.Rating;

        await _feedbackRepository.SaveFeedbackAsync(modelToUpdate);
    }

    private static long GetFeedbackSortValue(Feedback feedback, SortingOrder sortOrder)
    {
        return sortOrder switch
        {
            SortingOrder.TopRatedFirst => -feedback.Rating,
            SortingOrder.OldestFirst => feedback.CreatedAt.Ticks,
            SortingOrder.NewestFirst => -feedback.CreatedAt.Ticks,
            SortingOrder.LowRatedFirst => feedback.Rating,
            _ => feedback.CreatedAt.Ticks
        };
    }

    public async Task<List<FeedbackResponseItem>> GetFeedbackForReservationAsync(string reservationId)
    {
        var feedback = await _feedbackRepository.GetFeedbackByReservationIdAsync(reservationId);
        return feedback is null or { Count: 0 }
            ? []
            : [.. feedback.Select(f => f.ToResponseItem())];
    }
}