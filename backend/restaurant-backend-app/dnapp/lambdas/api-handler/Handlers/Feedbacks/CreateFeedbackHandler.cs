using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Feedbacks;
using APIHandler.Models.Users;
using APIHandler.Services.Auth;
using APIHandler.Services.Feedbacks;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Feedbacks;

public class CreateFeedbackHandler : IApiRequestHandler
{
    private readonly IFeedbackService _feedbackService;
    private readonly IUserService _userService;

    public CreateFeedbackHandler(IFeedbackService feedbackService, IUserService userService)
    {
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!TryParseRequestBody(request, out var feedbackRequest, out var errorMessage))
        {
            return ResponseBuilder.CreateErrorResponse(400, errorMessage!, context.Logger);
        }

        Roles? userRole = await GetUserRoleAsync(feedbackRequest!.UserId);
        if (userRole is not null or Roles.Customer)
        {
            return ResponseBuilder.CreateErrorResponse(403, "Only customers can create feedback.", context.Logger);
        }

        var existingFeedback = await _feedbackService.GetFeedbackForReservationAsync(feedbackRequest!.ReservationId);
        if (existingFeedback.Any(f => f.Type == feedbackRequest.Type))
        {
            return ResponseBuilder.CreateErrorResponse(409, "Feedback of this type already exists for the reservation. Please consider editing the existing feedback.", context.Logger);
        }

        try
        {
            var createdFeedback = await _feedbackService.CreateFeedbackAsync(feedbackRequest);
            return ResponseBuilder.CreateSuccessResponse(createdFeedback);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }

    private bool TryParseRequestBody(
        APIGatewayProxyRequest request,
        out CreateFeedbackRequest? feedbackRequest,
        out string? errorMessage)
    {
        try
        {
            feedbackRequest = JsonSerializer.Deserialize<CreateFeedbackRequest>(request.Body);
            errorMessage = null;
            return feedbackRequest is not null;
        }
        catch (Exception ex)
        {
            feedbackRequest = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    private async Task<Roles?> GetUserRoleAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }
        return await _userService.GetUserRoleAsync(userId);
    }
}