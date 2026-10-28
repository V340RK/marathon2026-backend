using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Services.Dishes;
using APIHandler.Services.Notifications;
using APIHandler.Services.Orders;
using AutoFixture.Xunit2;
using Moq;

namespace APIHandler.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IReservationRepository> _mockReservationRepository;
    private readonly Mock<IDishService> _mockDishService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILambdaLogger> _mockLogger;
    private readonly Mock<ILambdaContext> _mockLambdaContext;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockReservationRepository = new Mock<IReservationRepository>();
        _mockDishService = new Mock<IDishService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILambdaLogger>();

        _mockLambdaContext = new Mock<ILambdaContext>();
        _mockLambdaContext.Setup(c => c.Logger).Returns(_mockLogger.Object);

        _orderService = new OrderService(
            _mockOrderRepository.Object,
            _mockReservationRepository.Object,
            _mockDishService.Object,
            _mockNotificationService.Object
        );
    }

    [Theory]
    [AutoData]
    public async Task GetOrdersByCustomerIdAsync_ShouldReturnOrders(Order order)
    {
        var customerId = "customer-123";
        order.OrderId = "order-1";
        order.CustomerId = customerId;
        var expectedOrders = new List<Order> { order };

        _mockOrderRepository.Setup(r => r.GetOrdersByCustomerIdAsync(customerId)).ReturnsAsync(expectedOrders);

        var result = await _orderService.GetOrdersByCustomerIdAsync(customerId);

        Assert.NotNull(result);
        Assert.Single(result);
        _mockOrderRepository.Verify(r => r.GetOrdersByCustomerIdAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task GetOrdersByCustomerIdAsync_ShouldReturnEmptyList_WhenNoOrdersFound()
    {
        var customerId = "customer-123";
        _mockOrderRepository.Setup(r => r.GetOrdersByCustomerIdAsync(customerId)).ReturnsAsync(new List<Order>());

        var result = await _orderService.GetOrdersByCustomerIdAsync(customerId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmPreOrderDishAsync_ShouldMarkDishConfirmed_AndSubmitOrderIfAllProcessed(Reservation reservation, Order order)
    {
        var callerId = "waiter-1";
        var reservationId = "res-1";
        var dishId = "dish-1";
        reservation.Id = reservationId;
        reservation.TableId = "table-1";
        reservation.WaiterId = callerId;
        reservation.StartTime = DateTime.UtcNow.AddHours(1);

        order.OrderId = "ord-1";
        order.Status = OrderStatus.Created;
        order.Dishes = new List<OrderItem> { new OrderItem { DishId = dishId, State = OrderItemState.Pending } };

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        await _orderService.ConfirmPreOrderDishAsync(callerId, reservationId, dishId, _mockLogger.Object);

        Assert.Equal(OrderItemState.Confirmed, order.Dishes[0].State);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmPreOrderDishAsync_ShouldNotSubmitOrder_IfPendingDishesRemain(Reservation reservation, Order order)
    {
        var callerId = "waiter-1";
        var reservationId = "res-1";
        var dishId = "dish-1";
        reservation.WaiterId = callerId;
        reservation.StartTime = DateTime.UtcNow.AddHours(1);
        order.OrderId = "ord-1";
        order.Status = OrderStatus.Created;
        order.Dishes = new List<OrderItem>
        {
            new OrderItem { DishId = dishId, State = OrderItemState.Pending },
            new OrderItem { DishId = "dish-2", State = OrderItemState.Pending }
        };

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        await _orderService.ConfirmPreOrderDishAsync(callerId, reservationId, dishId, _mockLogger.Object);

        Assert.Equal(OrderItemState.Confirmed, order.Dishes[0].State);
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmPreOrderDishAsync_ShouldThrowActionForbiddenException_WhenWrongWaiter(Reservation reservation)
    {
        reservation.WaiterId = "correct-waiter";
        reservation.StartTime = DateTime.UtcNow.AddHours(1);
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(It.IsAny<string>())).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _orderService.ConfirmPreOrderDishAsync("wrong-waiter", reservation.Id, "dish-1", _mockLogger.Object));
    }

    [Theory]
    [AutoData]
    public async Task ConfirmPreOrderDishAsync_ShouldThrowDishNotFoundException_WhenDishNotFound(Reservation reservation, Order order)
    {
        var callerId = "waiter-1";
        var reservationId = reservation.Id;
        reservation.WaiterId = callerId;
        reservation.StartTime = DateTime.UtcNow.AddHours(1);
        order.OrderId = "ord-1";
        order.Dishes = new List<OrderItem>();

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        await Assert.ThrowsAsync<DishNotFoundException>(() =>
            _orderService.ConfirmPreOrderDishAsync(callerId, reservationId, "nonexistent-dish", _mockLogger.Object));
    }

    [Theory]
    [AutoData]
    public async Task CancelPreOrderDishAsync_ShouldMarkDishCanceled_AndSubmitOrderIfAllProcessed(Reservation reservation, Order order)
    {
        var callerId = "waiter-1";
        var reservationId = reservation.Id;
        var dishToCancelId = "dish-1";
        reservation.WaiterId = callerId;
        reservation.StartTime = DateTime.UtcNow.AddHours(1);
        order.OrderId = "ord-1";
        order.Status = OrderStatus.Created;
        order.Dishes = new List<OrderItem>
        {
            new OrderItem { DishId = dishToCancelId, State = OrderItemState.Pending },
            new OrderItem { DishId = "dish-2", State = OrderItemState.Confirmed }
        };

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(It.IsAny<string>())).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(It.IsAny<string>())).ReturnsAsync(order);

        await _orderService.CancelPreOrderDishAsync(callerId, reservationId, dishToCancelId, _mockLogger.Object);

        Assert.Equal(OrderItemState.Canceled, order.Dishes[0].State);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task CancelPreOrderDishAsync_ShouldNotSubmitOrder_IfPendingDishesRemain(Reservation reservation, Order order)
    {
        var callerId = "waiter-1";
        var reservationId = reservation.Id;
        var dishId = "dish-1";
        reservation.WaiterId = callerId;
        reservation.StartTime = DateTime.UtcNow.AddHours(1);
        order.OrderId = "ord-1";
        order.Status = OrderStatus.Created;
        order.Dishes = new List<OrderItem>
        {
            new OrderItem { DishId = dishId, State = OrderItemState.Pending },
            new OrderItem { DishId = "dish-2", State = OrderItemState.Pending }
        };

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(It.IsAny<string>())).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(It.IsAny<string>())).ReturnsAsync(order);

        await _orderService.CancelPreOrderDishAsync(callerId, reservationId, dishId, _mockLogger.Object);

        Assert.Equal(OrderItemState.Canceled, order.Dishes[0].State);
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Theory]
    [AutoData]
    public async Task CancelPreOrderDishAsync_ShouldThrowDishNotFoundException_WhenDishNotFound(Reservation reservation, Order order)
    {
        var callerId = "waiter-1";
        var reservationId = reservation.Id;
        reservation.WaiterId = callerId;
        reservation.StartTime = DateTime.UtcNow.AddHours(1);
        order.OrderId = "ord-1";
        order.Dishes = new List<OrderItem>();

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(It.IsAny<string>())).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(It.IsAny<string>())).ReturnsAsync(order);

        await Assert.ThrowsAsync<DishNotFoundException>(() =>
            _orderService.CancelPreOrderDishAsync(callerId, reservationId, "nonexistent-dish", _mockLogger.Object));
    }

    [Theory]
    [AutoData]
    public async Task GetOrderByReservationIdAsync_ShouldReturnOrder_WhenFoundAndNotCancelled(Order order)
    {
        order.Status = OrderStatus.Created;
        var reservationId = order.ReservationId;
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        var result = await _orderService.GetOrderByReservationIdAsync(reservationId);

        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Created, result.Status);
    }

    [Theory]
    [AutoData]
    public async Task GetOrderByReservationIdAsync_ShouldReturnNull_WhenOrderIsCancelled()
    {
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);

        var result = await _orderService.GetOrderByReservationIdAsync("res-1");

        Assert.Null(result);
    }

    [Theory]
    [AutoData]
    public async Task GetOrderByReservationIdAsync_ShouldReturnNull_WhenNoOrderExists()
    {
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync("res-1")).ReturnsAsync((Order?)null);

        var result = await _orderService.GetOrderByReservationIdAsync("res-1");

        Assert.Null(result);
    }

    [Theory]
    [AutoData]
    public async Task AddOrderAsync_ShouldCreateOrder_WhenValid(Order order, Reservation reservation)
    {
        order.ReservationId = reservation.Id;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);
        reservation.WaiterId = "waiter-1";

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync((Order?)null);

        await _orderService.AddOrderAsync(order);

        Assert.Equal(OrderStatus.Created, order.Status);
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
        _mockNotificationService.Verify(n => n.CreateOrderNotificationAsync("waiter-1", order.OrderId, reservation.Id), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task AddOrderAsync_ShouldThrowException_WhenActiveOrderExists(Order order, Reservation reservation)
    {
        order.ReservationId = reservation.Id;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);
        var existingOrder = new Order
        {
            OrderId = "existing-ord",
            Status = OrderStatus.Created,
            CustomerId = "cust-123",
            ReservationId = reservation.Id,
            ReservationAddress = "test",
            ReservationTime = "test",
            Dishes = new List<OrderItem>()
        };

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync(existingOrder);

        await Assert.ThrowsAsync<ValidationException>(() => _orderService.AddOrderAsync(order));
    }

    [Theory]
    [AutoData]
    public async Task AddOrderAsync_ShouldThrowException_WhenPastDeadline(Order order, Reservation reservation)
    {
        order.ReservationId = reservation.Id;
        reservation.StartTime = DateTime.UtcNow.AddMinutes(10);

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservation.Id)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ValidationException>(() => _orderService.AddOrderAsync(order));
    }

    [Theory]
    [AutoData]
    public async Task AddOrderForWaiterAsync_ShouldCreateOrder_WhenValid(Order order, Reservation reservation)
    {
        order.ReservationId = reservation.Id;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync((Order?)null);

        await _orderService.AddOrderForWaiterAsync(order);

        Assert.Equal(OrderStatus.Created, order.Status);
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task AddOrderForWaiterAsync_ShouldThrowException_WhenOrderExists(Order order, Reservation reservation)
    {
        order.ReservationId = reservation.Id;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);
        var existingOrder = new Order
        {
            OrderId = "existing-ord",
            Status = OrderStatus.Created,
            CustomerId = "cust-123",
            ReservationId = reservation.Id,
            ReservationAddress = "test",
            ReservationTime = "test",
            Dishes = new List<OrderItem>()
        };

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync(existingOrder);

        await Assert.ThrowsAsync<ValidationException>(() => _orderService.AddOrderForWaiterAsync(order));
    }

    [Theory]
    [AutoData]
    public async Task AddOrderForWaiterAsync_ShouldThrowException_WhenReservationNotFound(Order order)
    {
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(order.ReservationId)).ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _orderService.AddOrderForWaiterAsync(order));
    }

    [Theory]
    [AutoData]
    public async Task EditPreOrderAsync_ShouldUpdateDishes_WhenValid(Reservation reservation, Order order, Dish availableDish)
    {
        var customerId = "cust-1";
        var reservationId = reservation.Id;
        var newDishes = new List<OrderItem> { new OrderItem { DishId = availableDish.Id, Quantity = 2 } };
        reservation.CustomerId = customerId;
        reservation.Status = ReservationStatus.Reserved;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);
        order.OrderId = "ord-1";
        order.Dishes = new List<OrderItem>();
        availableDish.State = DishState.Available;

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);
        _mockDishService.Setup(s => s.GetDishByIdAsync(availableDish.Id)).ReturnsAsync(availableDish);

        await _orderService.EditPreOrderAsync(customerId, reservationId, newDishes, _mockLambdaContext.Object);

        _mockOrderRepository.Verify(r => r.SaveOrderAsync(It.Is<Order>(o => o.Dishes.Count == 1 && o.Dishes[0].DishId == availableDish.Id)), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task EditPreOrderAsync_ShouldThrowActionForbiddenException_WhenWrongCustomer(Reservation reservation)
    {
        reservation.CustomerId = "correct-customer";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _orderService.EditPreOrderAsync("wrong-customer", reservationId, new List<OrderItem>(), _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task EditPreOrderAsync_ShouldThrowValidationException_WhenStatusNotReserved(Reservation reservation)
    {
        reservation.CustomerId = "cust-1";
        reservation.Status = ReservationStatus.InProgress;
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _orderService.EditPreOrderAsync("cust-1", reservationId, new List<OrderItem>(), _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task EditPreOrderAsync_ShouldThrowValidationException_WhenDishValidationFails(Reservation reservation, Order order)
    {
        var customerId = "cust-1";
        var reservationId = reservation.Id;
        var dishIdToFetch = "new-dish";
        var newDishes = new List<OrderItem> { new OrderItem { DishId = dishIdToFetch, Quantity = 2 } };
        reservation.CustomerId = customerId;
        reservation.Status = ReservationStatus.Reserved;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);
        order.OrderId = "ord-1";

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);
        _mockDishService.Setup(s => s.GetDishByIdAsync(dishIdToFetch)).ReturnsAsync((Dish?)null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _orderService.EditPreOrderAsync(customerId, reservationId, newDishes, _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task EditPreOrderForWaiterAsync_ShouldUpdateDishes_WhenValid(Reservation reservation, Order order, Dish availableDish)
    {
        var waiterId = "waiter-1";
        var reservationId = reservation.Id;
        var newDishes = new List<OrderItem> { new OrderItem { DishId = availableDish.Id, Quantity = 1 } };
        reservation.WaiterId = waiterId;
        reservation.Status = ReservationStatus.InProgress;
        order.OrderId = "ord-1";
        order.Dishes = new List<OrderItem>();
        availableDish.State = DishState.Available;

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);
        _mockDishService.Setup(s => s.GetDishByIdAsync(availableDish.Id)).ReturnsAsync(availableDish);

        await _orderService.EditPreOrderForWaiterAsync(waiterId, reservationId, newDishes, _mockLambdaContext.Object);

        _mockOrderRepository.Verify(r => r.SaveOrderAsync(It.Is<Order>(o => o.Dishes.Count == 1)), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task EditPreOrderForWaiterAsync_ShouldThrowActionForbiddenException_WhenWrongWaiter(Reservation reservation)
    {
        reservation.WaiterId = "correct-waiter";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _orderService.EditPreOrderForWaiterAsync("wrong-waiter", reservationId, new List<OrderItem>(), _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task EditPreOrderForWaiterAsync_ShouldThrowValidationException_WhenStatusNotInProgress(Reservation reservation)
    {
        reservation.WaiterId = "waiter-1";
        reservation.Status = ReservationStatus.Reserved;
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _orderService.EditPreOrderForWaiterAsync("waiter-1", reservationId, new List<OrderItem>(), _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task CancelPreOrderAsync_ShouldCancelOrder_WhenValid(Reservation reservation, Order order)
    {
        var customerId = "cust-1";
        var reservationId = reservation.Id;
        reservation.CustomerId = customerId;
        order.OrderId = "ord-1";
        order.Status = OrderStatus.Created;

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        await _orderService.CancelPreOrderAsync(customerId, reservationId, _mockLambdaContext.Object);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task CancelPreOrderAsync_ShouldThrowActionForbiddenException_WhenWrongCustomer(Reservation reservation)
    {
        reservation.CustomerId = "legit-customer";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _orderService.CancelPreOrderAsync("hacker-1", reservationId, _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task CancelPreOrderAsync_ShouldThrowInvalidOperation_WhenOrderNotFound(Reservation reservation)
    {
        reservation.CustomerId = "cust-1";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _orderService.CancelPreOrderAsync("cust-1", reservationId, _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task SubmitPreOrderAsync_ShouldSubmitOrder_WhenValid(Reservation reservation, Order order)
    {
        var customerId = "cust-1";
        var reservationId = reservation.Id;
        reservation.CustomerId = customerId;
        order.OrderId = "ord-1";
        order.Status = OrderStatus.Created;

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        await _orderService.SubmitPreOrderAsync(customerId, reservationId, _mockLambdaContext.Object);

        Assert.Equal(OrderStatus.SubmittedByCustomer, order.Status);
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task SubmitPreOrderAsync_ShouldThrowActionForbiddenException_WhenWrongCustomer(Reservation reservation)
    {
        reservation.CustomerId = "correct-customer";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _orderService.SubmitPreOrderAsync("wrong-customer", reservationId, _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task SubmitPreOrderAsync_ShouldThrowValidationException_WhenOrderNotFound(Reservation reservation)
    {
        reservation.CustomerId = "cust-1";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _orderService.SubmitPreOrderAsync("cust-1", reservationId, _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task UnSubmitPreOrderAsync_ShouldRevertOrderStatus_WhenValid(Reservation reservation, Order order)
    {
        var customerId = "cust-1";
        var reservationId = reservation.Id;
        reservation.CustomerId = customerId;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);
        order.OrderId = "ord-1";
        order.Status = OrderStatus.SubmittedByCustomer;

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        await _orderService.UnSubmitPreOrderAsync(customerId, reservationId, _mockLambdaContext.Object);

        Assert.Equal(OrderStatus.Created, order.Status);
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task UnSubmitPreOrderAsync_ShouldThrowActionForbiddenException_WhenWrongCustomer(Reservation reservation)
    {
        reservation.CustomerId = "correct-customer";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _orderService.UnSubmitPreOrderAsync("wrong-customer", reservationId, _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task UnSubmitPreOrderAsync_ShouldThrowValidationException_WhenOrderNotFound(Reservation reservation)
    {
        reservation.CustomerId = "cust-1";
        var reservationId = reservation.Id;
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _orderService.UnSubmitPreOrderAsync("cust-1", reservationId, _mockLambdaContext.Object));
    }

    [Theory]
    [AutoData]
    public async Task UnSubmitPreOrderAsync_ShouldThrowException_WhenPastDeadline(Reservation reservation, Order order)
    {
        var customerId = "cust-1";
        var reservationId = reservation.Id;
        reservation.CustomerId = customerId;
        reservation.StartTime = DateTime.UtcNow.AddMinutes(10);
        order.OrderId = "ord-1";
        order.Status = OrderStatus.SubmittedByCustomer;

        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(reservationId)).ReturnsAsync(reservation);
        _mockOrderRepository.Setup(r => r.GetOrderByReservationIdAsync(reservationId)).ReturnsAsync(order);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _orderService.UnSubmitPreOrderAsync(customerId, reservationId, _mockLambdaContext.Object));
    }
}