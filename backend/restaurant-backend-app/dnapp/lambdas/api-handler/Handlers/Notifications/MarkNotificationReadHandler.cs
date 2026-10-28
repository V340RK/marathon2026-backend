using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.Notifications;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Notifications;

public class MarkNotificationReadHandler : IApiRequestHandler
{
    private readonly INotificationService _notificationService;

    public MarkNotificationReadHandler(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            ValidateRequest(request);
            var (userId, notificationId) = ExtractParameters(request);

            var notification = await _notificationService.GetNotificationByIdAsync(notificationId) 
                ?? throw new NotificationNotFoundException();

            if (!string.Equals(notification.UserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ActionForbiddenException("You are not authorized to modify this notification");
            }

            await _notificationService.MarkAsReadAsync(notificationId);

            context.Logger.LogLine($"Notification {notificationId} marked as read by user {userId}");

            return ResponseBuilder.CreateSuccessResponse("Notification marked as read", 200);
        }
        catch (NotificationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ResponseBuilder.CreateErrorResponse(401, ex.Message, context.Logger);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }

    private void ValidateRequest(APIGatewayProxyRequest request)
    {
        if (request == null)
            throw new ValidationException("Request is null");

        if (request.PathParameters == null || !request.PathParameters.ContainsKey("id"))
            throw new ValidationException("Notification ID is required");

        if (string.IsNullOrEmpty(request.PathParameters["id"]))
            throw new ValidationException("Notification ID cannot be empty");
    }

    private (string userId, string notificationId) ExtractParameters(APIGatewayProxyRequest request)
    {
        var notificationId = request.PathParameters["id"];
        var userId = UserContextHelper.GetAuthenticatedUserId(request)
            ?? throw new ValidationException("Unauthorized.");

        return (userId, notificationId);
    }
}

