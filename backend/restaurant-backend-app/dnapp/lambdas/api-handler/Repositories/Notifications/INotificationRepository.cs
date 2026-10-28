using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Notifications;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(string userId);
    Task<Notification?> GetNotificationByIdAsync(string notificationId);
    Task SaveNotificationAsync(Notification notification);
}
