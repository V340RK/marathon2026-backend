using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.Orders;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Order;

public class CancelPreOrderDishHandler : IApiRequestHandler
{
    private readonly IOrderService _orderService;

    public CancelPreOrderDishHandler(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var callerId = UserContextHelper.GetAuthenticatedUserId(request);
        if (callerId is null)
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);
        }

        var isWaiter = UserContextHelper.IsRequestFromWaiter(request);
        if (!isWaiter)
        {
            return ResponseBuilder.CreateErrorResponse(403, "Access Forbidden: You can't cancel pre order.", context.Logger);
        }

        var reservationId = request.PathParameters["reservationId"];
        if (DataValidationHelper.IsMissing(reservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing reservation ID.", context.Logger);
        }

        var dishId = request.PathParameters["dishId"];
        if (DataValidationHelper.IsMissing(dishId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing dish ID.", context.Logger);
        }

        try
        {
            await _orderService.CancelPreOrderDishAsync(callerId, reservationId, dishId, context.Logger);
            return ResponseBuilder.CreateSuccessResponse("Pre order dish canceled successfully");
        }
        catch (ReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (DishNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
    }
}
