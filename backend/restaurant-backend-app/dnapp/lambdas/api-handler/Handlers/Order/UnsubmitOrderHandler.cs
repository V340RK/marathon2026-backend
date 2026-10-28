using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.Orders;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Order;

public class UnsubmitOrderHandler : IApiRequestHandler
{
    private readonly IOrderService _orderService;

    public UnsubmitOrderHandler(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            ValidateRequest(request);
            var (customerId, reservationId) = ExtractParameters(request);
            await _orderService.UnSubmitPreOrderAsync(customerId, reservationId, context);

            return ResponseBuilder.CreateSuccessResponse("Pre-order unsubmitted successfully.", 200);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ResponseBuilder.CreateErrorResponse(401, ex.Message, context.Logger);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
        catch (ReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }

    private static void ValidateRequest(APIGatewayProxyRequest request)
    {
        if (request == null)
            throw new InvalidOperationException("Request is null");
            
        if (request.PathParameters == null || !request.PathParameters.ContainsKey("reservationId"))
            throw new InvalidOperationException("Reservation ID is required.");
            
        if (string.IsNullOrEmpty(request.PathParameters["reservationId"]))
            throw new ValidationException("Reservation ID cannot be empty.");
    }

    private static (string customerId, string reservationId) ExtractParameters(APIGatewayProxyRequest request)
    {
        var reservationId = request.PathParameters["reservationId"];
        var customerId = UserContextHelper.GetAuthenticatedUserId(request)
            ?? throw new UnauthorizedAccessException("Unauthorized.");

        return (customerId, reservationId);
    }
}