using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.Orders;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Order;

public class CancelPreOrderHandler : IApiRequestHandler
{
    private readonly IOrderService _orderService;

    public CancelPreOrderHandler(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            ValidateRequest(request);
            var (customerId, reservationId) = ExtractParameters(request);
            await _orderService.CancelPreOrderAsync(customerId, reservationId, context);

            return ResponseBuilder.CreateSuccessResponse("Pre-order canceled successfully.", 200);
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

    private void ValidateRequest(APIGatewayProxyRequest request)
    {
        if (request == null)
            throw new InvalidOperationException("Request is null");
            
        if (request.PathParameters == null || !request.PathParameters.ContainsKey("reservationId"))
            throw new InvalidOperationException("Reservation ID is required.");
            
        if (string.IsNullOrEmpty(request.PathParameters["reservationId"]))
            throw new ValidationException("Reservation ID cannot be empty.");
    }

    private (string customerId, string reservationId) ExtractParameters(APIGatewayProxyRequest request)
    {
        var reservationId = request.PathParameters["reservationId"];
        var customerId = UserContextHelper.GetAuthenticatedUserId(request)
            ?? throw new UnauthorizedAccessException("Unauthorized.");

        return (customerId, reservationId);
    }
}