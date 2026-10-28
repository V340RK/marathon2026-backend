using APIHandler.Entities;
using APIHandler.Models.Notification;
using System.Collections.Generic;
using System.Linq;

namespace APIHandler.Mappers;

public static class NotificationMapper
{
    public static NotificationResponse ToNotificationResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Message = notification.Message,
            OrderId = notification.OrderId,
            ReservationId = notification.ReservationId,
            CreatedAt = notification.CreatedAt,
            IsRead = notification.IsRead
        };
    }
    public static List<NotificationResponse> ToNotificationResponseList(IEnumerable<Notification> notifications)
    {
        return notifications.Select(ToNotificationResponse).ToList();
    }
}