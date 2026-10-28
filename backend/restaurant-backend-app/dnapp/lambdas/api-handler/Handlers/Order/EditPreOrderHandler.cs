using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Orders;
using APIHandler.Models.Users;
using APIHandler.Services.Orders;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Order;

public class EditPreOrderHandler : IApiRequestHandler
{
    private readonly IOrderService _orderService;

    public EditPreOrderHandler(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var userRole = GetUserRole(request);
            var (userId, reservationId, editRequest) =
                ValidateAndPrepareRequest(request);

            return userRole switch
            {
                var role when role == Roles.Waiter.ToString() => await HandleWaiterRequestAsync(userId, reservationId, editRequest, context),
                var role when role == Roles.Customer.ToString() => await HandleCustomerRequestAsync(userId, reservationId, editRequest, context),
                _ => ResponseBuilder.CreateErrorResponse(403, "Forbidden: Invalid user role.", context.Logger)
            };
        }
        catch (ReservationNotFoundException ex)
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

    private async Task<APIGatewayProxyResponse> HandleCustomerRequestAsync(string customerId,
        string reservationId,
        EditPreOrderRequest editRequest,
        ILambdaContext context)
    {
        await _orderService.EditPreOrderAsync(customerId, reservationId, editRequest.OrderItems, context);
        return ResponseBuilder.CreateSuccessResponse("Pre-order updated successfully.", 200);
    }

    private async Task<APIGatewayProxyResponse> HandleWaiterRequestAsync(string waiterId,
        string reservationId,
        EditPreOrderRequest editRequest,
        ILambdaContext context)
    {
        await _orderService.EditPreOrderForWaiterAsync(waiterId, reservationId, editRequest.OrderItems, context);
        return ResponseBuilder.CreateSuccessResponse("Order updated successfully.", 200);
    }

    private static (string UserId, string ReservationId, EditPreOrderRequest EditRequest)
        ValidateAndPrepareRequest(APIGatewayProxyRequest request)
    {
        var userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (string.IsNullOrEmpty(userId))
        {
            throw new ValidationException("Unauthorized.");
        }

        if (!request.PathParameters.TryGetValue("reservationId", out var reservationId) ||
            string.IsNullOrEmpty(reservationId))
        {
            throw new ValidationException("ReservationId is required in path parameters.");
        }

        if (!TryParseRequestBody(request.Body, out var editRequest))
        {
            throw new ValidationException("Invalid request body.");
        }

        return (userId, reservationId, editRequest!);
    }

    private static bool TryParseRequestBody(string body, out EditPreOrderRequest? editRequest)
    {
        try
        {
            editRequest = JsonSerializer.Deserialize<EditPreOrderRequest>(body);
            if (editRequest == null || editRequest.OrderItems == null || editRequest.OrderItems.Count == 0)
            {
                return false;
            }
            return true;
        }
        catch (JsonException)
        {
            editRequest = null;
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