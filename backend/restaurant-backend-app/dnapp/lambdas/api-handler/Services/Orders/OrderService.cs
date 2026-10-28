using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Services.Dishes;
using APIHandler.Services.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.Orders;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IDishService _dishService;
    private readonly INotificationService _notificationService;

    public OrderService(
        IOrderRepository orderRepository,
        IReservationRepository reservationRepository,
        IDishService dishService,
        INotificationService notificationService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _dishService = dishService ?? throw new ArgumentNullException(nameof(dishService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId)
    {
        return await _orderRepository.GetOrdersByCustomerIdAsync(customerId);
    }

    public async Task ConfirmPreOrderDishAsync(string callerId, string reservationId, string dishId, ILambdaLogger logger)
    {
        var reservation = await LoadReservationAsync(reservationId);

        EnsureCallerIsAssignedWaiter(callerId, reservation);
        EnsurePreOrderEditable(reservation.StartTime);

        var order = await LoadOrderByReservationIdAsync(reservationId);
        bool shouldSubmit = ConfirmDishInOrder(order, dishId);

        if (shouldSubmit)
        {
            order.Status = OrderStatus.Submitted;
            logger.Log($"Order {order.OrderId} marked as Submitted (all dishes confirmed/canceled and at least one confirmed).");
        }

        await _orderRepository.SaveOrderAsync(order);
        logger.Log($"Confirmed dish {dishId} in order {order.OrderId}.");
    }

    public async Task CancelPreOrderDishAsync(string callerId, string reservationId, string dishId, ILambdaLogger logger)
    {
        var reservation = await LoadReservationAsync(reservationId);

        EnsureCallerIsAssignedWaiter(callerId, reservation);
        EnsurePreOrderEditable(reservation.StartTime);

        var order = await LoadOrderByReservationIdAsync(reservationId);

        bool shouldSubmit = CancelDishInOrder(order, dishId);

        if (shouldSubmit)
        {
            order.Status = OrderStatus.Submitted;
            logger.Log($"Order {order.OrderId} marked as Submitted (all dishes confirmed/canceled and at least one confirmed).");
        }

        await _orderRepository.SaveOrderAsync(order);
        logger.Log($"Canceled dish {dishId} in order {order.OrderId}.");
    }

    public async Task<Order?> GetOrderByReservationIdAsync(string reservationId)
    {
        return await _orderRepository.GetOrderByReservationIdAsync(reservationId);
    }

    public async Task AddOrderAsync(Order order)
    {
        var reservation = await LoadReservationAsync(order.ReservationId);
        EnsurePreOrderEditable(reservation.StartTime);

        var existingActiveOrder = await GetActiveOrderByReservationIdAsync(order.ReservationId, throwIfNotFound: false);
        if (existingActiveOrder != null)
        {
            throw new ValidationException($"Active order already exists for reservation {order.ReservationId}.");
        }

        order.Status = OrderStatus.Created;
        await _orderRepository.SaveOrderAsync(order);

        if (!string.IsNullOrEmpty(reservation.WaiterId))
        {
            await _notificationService.CreateOrderNotificationAsync(
                reservation.WaiterId,
                order.OrderId,
                order.ReservationId);
        }
    }

    public async Task AddOrderForWaiterAsync(Order order)
    {
        _ = await LoadReservationAsync(order.ReservationId);

        var existingOrder = await GetOrderByReservationIdAsync(order.ReservationId);
        if (existingOrder != null)
        {
            throw new ValidationException($"Active order already exists for reservation {order.ReservationId}.");
        }

        order.Status = OrderStatus.Created;
        await _orderRepository.SaveOrderAsync(order);
    }

    public async Task EditPreOrderAsync(
        string customerId,
        string reservationId,
        List<OrderItem> newDishes,
        ILambdaContext context)
    {
        await EditPreOrderInternalAsync(
            reservationId,
            newDishes,
            context,
            reservation =>
            {
                if (!string.Equals(reservation.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
                    throw new ActionForbiddenException($"User '{customerId}' is not authorized to edit pre-order for reservation '{reservation.Id}' owned by '{reservation.CustomerId}'.");

                if (reservation.Status != ReservationStatus.Reserved)
                    throw new ValidationException($"Pre-order can be edited only if reservation status is 'Reserved'. Current status: '{reservation.Status}'.");

                EnsurePreOrderEditable(reservation.StartTime);
            });
    }

    public async Task EditPreOrderForWaiterAsync(
        string waiterId,
        string reservationId,
        List<OrderItem> newDishes,
        ILambdaContext context)
    {
        await EditPreOrderInternalAsync(
            reservationId,
            newDishes,
            context,
            reservation =>
            {
                if (EqualityHelper.NotEqualsIgnoreCase(reservation.WaiterId, waiterId))
                    throw new ActionForbiddenException($"Waiter '{waiterId}' is not authorized to edit pre-order for reservation '{reservation.Id}' assigned to '{reservation.WaiterId}'.");

                if (reservation.Status != ReservationStatus.InProgress)
                    throw new ValidationException($"Pre-order can be edited only if reservation status is 'InProgress'. Current status: '{reservation.Status}'.");
            });
    }

    public async Task CancelPreOrderAsync(string customerId, string reservationId, ILambdaContext context)
    {
        var reservation = await LoadReservationAsync(reservationId);

        if (!string.Equals(reservation.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ActionForbiddenException("You are not authorized to cancel this pre-order.");
        }

        var order = await GetActiveOrderByReservationIdAsync(reservationId, throwIfNotFound: true);
        order!.Status = OrderStatus.Cancelled;

        await _orderRepository.SaveOrderAsync(order);
        context.Logger.LogLine($"Pre-order {order.OrderId} for reservation {reservationId} cancelled by customer {customerId}.");
    }

    public async Task SubmitPreOrderAsync(string customerId, string reservationId, ILambdaContext context)
    {
        var reservation = await LoadReservationAsync(reservationId);

        if (!string.Equals(reservation.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ActionForbiddenException("You are not authorized to submit this pre-order.");
        }

        var order = await GetActiveOrderByReservationIdAsync(reservationId, throwIfNotFound: true);
        order!.Status = OrderStatus.SubmittedByCustomer;

        await _orderRepository.SaveOrderAsync(order);
        context.Logger.LogLine($"Pre-order {order.OrderId} for reservation {reservationId} submitted by customer {customerId}.");
    }

    public async Task UnSubmitPreOrderAsync(string customerId, string reservationId, ILambdaContext context)
    {
        var reservation = await LoadReservationAsync(reservationId);

        if (!string.Equals(reservation.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ActionForbiddenException("You are not authorized to submit this pre-order.");
        }

        var order = await GetOrderByReservationIdAsync(reservationId);
        if (order == null)
        {
            throw new ValidationException("No pre-order found for the given reservation.");
        }
        EnsurePreOrderEditable(reservation.StartTime);

        order.Status = OrderStatus.Created;

        await _orderRepository.SaveOrderAsync(order);
        context.Logger.LogLine($"Pre-order {order.OrderId} for reservation {reservationId} submitted by customer {customerId}.");
    }

    private async Task EditPreOrderInternalAsync(
    string reservationId,
    List<OrderItem> newDishes,
    ILambdaContext context,
    Action<Reservation> validateReservation)
    {
        var reservation = await LoadReservationAsync(reservationId);
        validateReservation(reservation);

        var orderItemsForValidation = newDishes.Select(d => new OrderItem
        {
            DishId = d.DishId,
            Quantity = d.Quantity
        }).ToList();

        var isValid = await ValidateDishes(orderItemsForValidation, context);
        if (!isValid)
            throw new ValidationException("One or more dishes in the request are invalid, unavailable, or have a quantity of 0.");

        var order = await GetActiveOrderByReservationIdAsync(reservationId, throwIfNotFound: true);
        order!.Dishes.Clear();

        foreach (var dish in newDishes)
        {
            order.Dishes.Add(new OrderItem
            {
                DishId = dish.DishId,
                Quantity = dish.Quantity,
                State = OrderItemState.Pending,
            });
        }

        await _orderRepository.SaveOrderAsync(order);
    }

    private async Task<Order?> GetActiveOrderByReservationIdAsync(string reservationId, bool throwIfNotFound = true)
    {
        var order = await _orderRepository.GetOrderByReservationIdAsync(reservationId);

        if (order == null && throwIfNotFound)
        {
            throw new ValidationException($"Active pre-order for reservation '{reservationId}' not found.");
        }

        return order;
    }

    private async Task<Reservation> LoadReservationAsync(string reservationId)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
        return reservation ?? throw new ReservationNotFoundException("Reservation not found.");
    }

    private async Task<Order> LoadOrderByReservationIdAsync(string reservationId)
    {
        var order = await _orderRepository.GetOrderByReservationIdAsync(reservationId);

        return order ?? throw new ValidationException("Order not found.");
    }

    private static void EnsurePreOrderEditable(DateTime reservationStartTime)
    {
        var cutoff = reservationStartTime.ToUniversalTime().AddMinutes(-30);
        if (DateTime.UtcNow > cutoff)
            throw new ValidationException($"Pre-orders can only be modified up to 30 minutes before the reservation start time ({reservationStartTime:HH:mm}).");
    }

    private static bool ConfirmDishInOrder(Order order, string dishId)
    {
        var dish = order.Dishes.FirstOrDefault(d => d.DishId == dishId)
            ?? throw new DishNotFoundException($"Dish {dishId} not found in order {order.OrderId}.");

        dish.State = OrderItemState.Confirmed;

        return ShouldMarkOrderAsSubmitted(order);
    }

    private static bool CancelDishInOrder(Order order, string dishId)
    {
        var dish = order.Dishes.FirstOrDefault(d => d.DishId == dishId)
            ?? throw new DishNotFoundException($"Dish {dishId} not found in order {order.OrderId}.");

        dish.State = OrderItemState.Canceled;

        return ShouldMarkOrderAsSubmitted(order);
    }

    private static void EnsureCallerIsAssignedWaiter(string callerId, Reservation reservation)
    {
        if (DataValidationHelper.IsMissing(reservation.WaiterId))
        {
            throw new ValidationException($"Reservation '{reservation.Id}' has no assigned waiter.");
        }

        if (!string.Equals(reservation.WaiterId, callerId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ActionForbiddenException($"Action forbidden: You '{callerId}' are not the assigned waiter '{reservation.WaiterId}' for reservation '{reservation.Id}'.");
        }
    }

    private async Task<bool> ValidateDishes(List<OrderItem> orderItems, ILambdaContext context)
    {
        foreach (var item in orderItems)
        {
            if (string.IsNullOrEmpty(item.DishId) || item.Quantity <= 0)
            {
                context.Logger.LogLine($"Invalid dish ID or quantity for dish: {item.DishId}, quantity: {item.Quantity}");
                return false;
            }

            var dish = await _dishService.GetDishByIdAsync(item.DishId);
            if (dish is null || dish.State != DishState.Available)
            {
                context.Logger.LogLine($"Dish with ID {item.DishId} is not available.");
                return false;
            }
        }
        return orderItems.Count > 0;
    }

    private static bool ShouldMarkOrderAsSubmitted(Order order)
    {
        bool anyConfirmed = order.Dishes.Any(d => d.State == OrderItemState.Confirmed);
        bool anyPending = order.Dishes.Any(d => d.State == OrderItemState.Pending);

        return anyConfirmed && !anyPending;
    }
}