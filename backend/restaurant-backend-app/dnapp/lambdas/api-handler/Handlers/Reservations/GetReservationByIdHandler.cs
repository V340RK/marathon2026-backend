using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Services.Locations;
using APIHandler.Services.Orders;
using APIHandler.Services.Reservations;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Reservations;

public class GetReservationByIdHandler : IApiRequestHandler
{
    private readonly IReservationService _reservationService;
    private readonly ILocationService _locationService;
    private readonly IOrderService _orderService;

    public GetReservationByIdHandler(IOrderService orderService, ILocationService locationService, IReservationService reservationService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var callerUserId = UserContextHelper.GetAuthenticatedUserId(request);
        if (callerUserId is null)
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized: User ID is missing.", context.Logger);
        }

        if (request.PathParameters == null ||
            !request.PathParameters.TryGetValue("reservationId", out var reservationId) ||
            DataValidationHelper.IsMissing(reservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "No reservation id was found in request.", context.Logger);
        }

        var reservation = await _reservationService.GetReservationByIdAsync(reservationId);

        if (reservation == null)
        {
            return ResponseBuilder.CreateErrorResponse(404, $"Reservation with id '{reservationId}' was not found.", context.Logger);
        }

        var location = await _locationService.GetLocationByTableIdAsync(reservation.TableId);

        if (location == null)
        {
            return ResponseBuilder.CreateErrorResponse(404, $"Location with id '{reservation.TableId}' was not found.", context.Logger);
        }

        var order = await _orderService.GetOrderByReservationIdAsync(reservation.Id);

        var reservationResponse = ReservationMapper.ToReservationResponse(reservation, location.Address, order);
        return ResponseBuilder.CreateSuccessResponse(reservationResponse);
    }
}
