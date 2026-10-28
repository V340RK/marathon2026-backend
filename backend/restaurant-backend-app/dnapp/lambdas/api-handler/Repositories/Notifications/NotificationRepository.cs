using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using APIHandler.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Notifications;

public class NotificationRepository : INotificationRepository
{
    private static readonly string NotificationsTableName = ResourceNames.NotificationsTableName;
    private static readonly string UserIdIndex = ResourceNames.NotificationsUserIdIndex;

    private readonly IDynamoDBContext _dynamoDbContext;

    public NotificationRepository(IDynamoDBContext dynamoDbContext)
    {
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(string userId)
    {
        var queryConfig = new QueryConfig
        {
            OverrideTableName = NotificationsTableName,
            IndexName = UserIdIndex
        };

        var query = _dynamoDbContext.QueryAsync<Notification>(userId, queryConfig);

        return await query.GetRemainingAsync();
    }

    public async Task<Notification?> GetNotificationByIdAsync(string notificationId)
    {
        return await _dynamoDbContext.LoadAsync<Notification>(
            notificationId, DynamoConfig.LoadConfig(NotificationsTableName));
    }

    public async Task SaveNotificationAsync(Notification notification)
    {
        await _dynamoDbContext.SaveAsync(notification, DynamoConfig.SaveConfig(NotificationsTableName));
    }
}
