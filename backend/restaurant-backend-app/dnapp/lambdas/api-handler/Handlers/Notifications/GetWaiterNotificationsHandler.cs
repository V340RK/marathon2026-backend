using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Services.Notifications;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Notifications;

public class GetWaiterNotificationsHandler : IApiRequestHandler
{
    private readonly INotificationService _notificationService;

    public GetWaiterNotificationsHandler(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request == null)
            return ResponseBuilder.CreateErrorResponse(400, "Request is null", context.Logger);

        string? userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (userId is null)
            return ResponseBuilder.CreateErrorResponse(403, "Unauthorized", context.Logger);

        var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);

        context.Logger.LogLine($"Found {notifications.Count()} notifications for user {userId}");

        var responseList = NotificationMapper.ToNotificationResponseList(notifications);

        return ResponseBuilder.CreateSuccessResponse(responseList, 200);
    }
}