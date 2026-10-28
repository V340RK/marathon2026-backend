using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Models.Orders;
using APIHandler.Models.Users;
using APIHandler.Services.Dishes;
using APIHandler.Services.Locations;
using APIHandler.Services.Orders;
using APIHandler.Services.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Order;

public class CreateOrderHandler : IApiRequestHandler
{
    private readonly IDishService _dishService;
    private readonly IReservationService _reservationService;
    private readonly ILocationService _locationService;
    private readonly IOrderService _orderService;

    public CreateOrderHandler(
        IDishService dishService,
        IReservationService reservationService,
        ILocationService locationService,
        IOrderService orderService)
    {
        _dishService = dishService ?? throw new ArgumentNullException(nameof(dishService));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        string userRole;
        try
        {
            userRole = GetUserRole(request);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }

        return userRole switch
        {
            var role when role == Roles.Waiter.ToString() => await HandleWaiterRequestAsync(request, context),
            var role when role == Roles.Customer.ToString() => await HandleCustomerRequestAsync(request, context),
            _ => ResponseBuilder.CreateErrorResponse(403, "Forbidden: Invalid user role.", context.Logger)
        };
    }

    private async Task<APIGatewayProxyResponse> HandleWaiterRequestAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var waiterId = UserContextHelper.GetAuthenticatedUserId(request)
                ?? throw new UnauthorizedAccessException("Unauthorized.");

            var (createOrderRequest, reservation, locationAddress, reservationTime) = await PrepareOrderDataAsync(request, context);

            if (EqualityHelper.NotEqualsIgnoreCase(reservation.WaiterId, waiterId))
            {
                return ResponseBuilder.CreateErrorResponse(
                    403,
                    "You are not authorized to modify orders for this reservation.",
                    context.Logger);
            }

            if (reservation.Status != ReservationStatus.InProgress)
            {
                return ResponseBuilder.CreateErrorResponse(400, "Order is allowed for modification only for reservations which have in 'InProgress' status.", context.Logger);
            }

            var order = OrderMapper.ToOrderEntity(
                createOrderRequest,
                reservation.CustomerId ?? string.Empty,
                locationAddress,
                reservationTime);

            await _orderService.AddOrderForWaiterAsync(order);

            var dishes = await LoadDishesAsync(order.Dishes);

            return ResponseBuilder.CreateSuccessResponse(OrderMapper.ToOrderResponse(order, dishes), 201);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error while creating order: {ex.Message}");
            return MapExceptionToResponse(ex, context);
        }
    }

    private async Task<APIGatewayProxyResponse> HandleCustomerRequestAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var customerId = UserContextHelper.GetAuthenticatedUserId(request)
                ?? throw new UnauthorizedAccessException("Unauthorized.");

            var (createOrderRequest, reservation, locationAddress, reservationTime) = await PrepareOrderDataAsync(request, context);

            if (EqualityHelper.NotEqualsIgnoreCase(reservation.CustomerId, customerId))
            {
                return ResponseBuilder.CreateErrorResponse(
                    403,
                    "You are not authorized to modify orders for this reservation.",
                    context.Logger);
            }

            if (reservation.Status != ReservationStatus.Reserved)
            {
                return ResponseBuilder.CreateErrorResponse(400, "Pre-order is allowed only for reservations in 'Reserved' status.", context.Logger);
            }

            var order = OrderMapper.ToOrderEntity(
                createOrderRequest,
                customerId,
                locationAddress,
                reservationTime);

            await _orderService.AddOrderAsync(order);

            var dishes = await LoadDishesAsync(order.Dishes);

            return ResponseBuilder.CreateSuccessResponse(OrderMapper.ToOrderResponse(order, dishes), 201);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error while placing pre-order: {ex.Message}");
            return MapExceptionToResponse(ex, context);
        }
    }

    private async Task<(CreateOrderRequest Request, Reservation Reservation, string LocationAddress, string ReservationTime)>
        PrepareOrderDataAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!TryParseRequestBody(request, out var createOrderRequest))
            throw new ValidationException("Invalid request body.");

        if (!await ValidateDishes(createOrderRequest!.OrderItems, context))
            throw new ValidationException("Invalid dish IDs in order items or some dishes aren't available.");

        var reservation = await _reservationService.GetReservationByIdAsync(createOrderRequest.ReservationId)
            ?? throw new ValidationException("Reservation not found.");

        var location = await _locationService.GetLocationByTableIdAsync(reservation.TableId)
            ?? throw new ValidationException("Location not found for the reservation's table.");

        var reservationTime = $"{reservation.StartTime:MMM dd, yyyy, h:mm tt} - {reservation.EndTime:h:mm tt}";

        return (createOrderRequest, reservation, location.Address, reservationTime);
    }

    private async Task<List<Dish>> LoadDishesAsync(IEnumerable<OrderItem> orderDishes)
    {
        var tasks = orderDishes.Select(d => _dishService.GetDishByIdAsync(d.DishId));
        var dishes = await Task.WhenAll(tasks);
        return dishes.ToList();
    }

    private static APIGatewayProxyResponse MapExceptionToResponse(Exception ex, ILambdaContext context)
    {
        return ex switch
        {
            ValidationException => ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger),
            UnauthorizedAccessException => ResponseBuilder.CreateErrorResponse(401, ex.Message, context.Logger),
            ActionForbiddenException => ResponseBuilder.CreateErrorResponse(401, ex.Message, context.Logger),
            _ => ResponseBuilder.CreateErrorResponse(500, $"Internal server error: {ex.Message}", context.Logger)
        };
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

    private static bool TryParseRequestBody(APIGatewayProxyRequest request, out CreateOrderRequest? createOrderRequest)
    {
        try
        {
            createOrderRequest = JsonSerializer.Deserialize<CreateOrderRequest>(request.Body);
            return createOrderRequest != null;
        }
        catch (JsonException)
        {
            createOrderRequest = null;
            return false;
        }
    }

    private static string GetUserRole(APIGatewayProxyRequest request)
    {
        if (request.RequestContext.Authorizer is null ||
            request.RequestContext.Authorizer.Claims is null ||
            !request.RequestContext.Authorizer.Claims.TryGetValue("cognito:groups", out var groups))
        {
            throw new UnauthorizedAccessException("User role not found in request context.");
        }

        return groups.Contains(Roles.Waiter.ToString())
            ? Roles.Waiter.ToString()
            : Roles.Customer.ToString();
    }
}