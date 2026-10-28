using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Notifications;

public interface INotificationService
{
    Task<Entities.Notification> CreateOrderNotificationAsync(string waiterId, string orderId, string reservationId);

    Task<IEnumerable<Entities.Notification>> GetUnreadNotificationsAsync(string userId);

    Task MarkAsReadAsync(string notificationId);

    Task<Entities.Notification?> GetNotificationByIdAsync(string notificationId);
}