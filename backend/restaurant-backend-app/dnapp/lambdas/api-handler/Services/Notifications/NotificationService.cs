using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Notifications;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IDishRepository _dishRepository;

    public NotificationService(
        INotificationRepository notificationRepository,
        IReservationRepository reservationRepository,
        IOrderRepository orderRepository,
        ILocationRepository locationRepository,
        IDishRepository dishRepository)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
    }

    public async Task<Entities.Notification> CreateOrderNotificationAsync(string waiterId, string orderId, string reservationId)
    {
        EnsureNotNull(waiterId, nameof(waiterId));
        EnsureNotNull(orderId, nameof(orderId));
        EnsureNotNull(reservationId, nameof(reservationId));

        var reservation = await LoadReservationAsync(reservationId);
        string customerId = reservation.CustomerId
             ?? throw new InvalidOperationException($"Reservation '{reservationId}' does not have a customer ID assigned.");

        var order = await LoadOrderAsync(orderId, customerId);
        var tableNumber = await GetTableNumberAsync(reservation.TableId);
        var dishesSummary = await BuildDishesSummaryAsync(order.Dishes);

        var message = BuildOrderNotificationMessage(orderId, reservation, tableNumber, dishesSummary);

        var notification = new Entities.Notification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = waiterId,
            Message = message,
            OrderId = orderId,
            ReservationId = reservationId,
            CreatedAt = DateTime.UtcNow
        };

        ValidateNotification(notification);
        Console.WriteLine($"Notification before save: {JsonSerializer.Serialize(notification)}");

        await _notificationRepository.SaveNotificationAsync(notification);
        return notification;
    }

    public async Task<IEnumerable<Entities.Notification>> GetUnreadNotificationsAsync(string userId)
    {
        EnsureNotNull(userId, nameof(userId));

        var results = await _notificationRepository.GetUnreadNotificationsByUserIdAsync(userId);
        return results.Where(n => !n.IsRead).Take(20);
    }

    public async Task MarkAsReadAsync(string notificationId)
    {
        EnsureNotNull(notificationId, nameof(notificationId));

        var notification = await GetNotificationByIdAsync(notificationId)
            ?? throw new InvalidOperationException($"Notification '{notificationId}' not found.");
        notification.IsRead = true;
        await _notificationRepository.SaveNotificationAsync(notification);
    }

    public async Task<Entities.Notification?> GetNotificationByIdAsync(string notificationId)
    {
        EnsureNotNull(notificationId, nameof(notificationId));

        return await _notificationRepository.GetNotificationByIdAsync(notificationId);
    }

    private async Task<Reservation> LoadReservationAsync(string reservationId)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
        return reservation ?? throw new ReservationNotFoundException($"Reservation with ID {reservationId} not found.");
    }

    private async Task<Order> LoadOrderAsync(string orderId, string customerId)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, customerId);
        return order ?? throw new ValidationException($"Order with ID {orderId} not found.");
    }

    private async Task<int> GetTableNumberAsync(string tableId)
    {
        if (string.IsNullOrEmpty(tableId)) return 0;

        var table = await _locationRepository.GetTableByIdAsync(tableId);

        if (table == null)
        {
            Console.WriteLine($"Table with ID {tableId} not found.");
            return 0;
        }

        return table.TableNumber;
    }

    private async Task<string> BuildDishesSummaryAsync(List<OrderItem> orderItems)
    {
        if (orderItems == null || orderItems.Count == 0)
            return "no dishes";

        var dishesSummary = new List<string>();

        foreach (var item in orderItems)
        {
            var dish = await _dishRepository.GetDishByIdAsync(item.DishId);
            var dishName = dish?.Name ?? "Unknown Dish";
            dishesSummary.Add($"{item.Quantity}x {dishName}");
        }

        return string.Join(", ", dishesSummary);
    }

    private static void ValidateNotification(Entities.Notification notification)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        EnsureNotNull(notification.UserId, "UserId");
        EnsureNotNull(notification.Message, "Message");
        EnsureNotNull(notification.OrderId, "OrderId");
        EnsureNotNull(notification.ReservationId, "ReservationId");
    }

    private string BuildOrderNotificationMessage(string orderId, Reservation reservation, int tableNumber, string dishesSummary)
    {
        return $"New order #{orderId} for Table {tableNumber} ({reservation.GuestCount} guests) at {reservation.StartTime:MMM dd, yyyy, h:mm tt}. Ordered: {dishesSummary}.";
    }

    private static void EnsureNotNull(string? value, string paramName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(paramName);
    }
}