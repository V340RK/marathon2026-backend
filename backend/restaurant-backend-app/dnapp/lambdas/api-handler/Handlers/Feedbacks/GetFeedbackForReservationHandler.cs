using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Models.Feedbacks;
using APIHandler.Services.Auth;
using APIHandler.Services.Feedbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Feedbacks;

public class GetFeedbackForReservationHandler : IApiRequestHandler
{
    private readonly IFeedbackService _feedbackService;
    private readonly IUserService _userService;

    public GetFeedbackForReservationHandler(IFeedbackService feedbackService, IUserService userService)
    {
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!TryGetReservationIdFromPath(request, out var reservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Reservation ID is reuired in the path.", context.Logger);
        }

        List<FeedbackResponseItem> feedbackItems = await _feedbackService.GetFeedbackForReservationAsync(reservationId!);

        foreach (var item in feedbackItems)
        {
            item.Username = item.UserId is not null
                ? await _userService.GetUserName(item.UserId) ?? "Anonymous User"
                : "Anonymous User";
        }

        var responseBody = new
        {
            service = feedbackItems.SingleOrDefault(f => f.Type == FeedbackType.Service),
            cuisineExperience = feedbackItems.SingleOrDefault(f => f.Type == FeedbackType.CuisineExperience)
        };

        return ResponseBuilder.CreateSuccessResponse(responseBody);
    }

    private bool TryGetReservationIdFromPath(APIGatewayProxyRequest request, out string? reservationId)
    {
        if (request.PathParameters is not null && request.PathParameters.TryGetValue("reservationId", out reservationId))
        {
            return !string.IsNullOrWhiteSpace(reservationId);
        }

        reservationId = null;
        return false;
    }
}