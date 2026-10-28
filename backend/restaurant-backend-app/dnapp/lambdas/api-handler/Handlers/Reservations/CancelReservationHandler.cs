using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Reservations;
using APIHandler.Models.Users;
using APIHandler.Services.Reservations;
using APIHandler.Services.Waiters;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Reservations;

public class CancelReservationHandler : IApiRequestHandler
{
    private readonly IWaiterService _waiterService;
    private readonly IReservationService _reservationService;

    public CancelReservationHandler(IWaiterService waiterService, IReservationService reservationService)
    {
        _waiterService = waiterService ?? throw new ArgumentNullException(nameof(waiterService));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var cancelRequest = ParseRequestBody(request.Body);
            if (string.IsNullOrWhiteSpace(cancelRequest?.ReservationId))
            {
                return ResponseBuilder.CreateErrorResponse(400, "ReservationId is required", context.Logger);
            }

            var claims = request.RequestContext.Authorizer?.Claims;
            if (claims is not { Count: > 0 } || !claims.TryGetValue("sub", out var userId))
            {
                return ResponseBuilder.CreateErrorResponse(401, "Unauthorized: User ID is missing.", context.Logger);
            }

            var roles = RoleHelper.ParseRoles(claims.TryGetValue("cognito:groups", out var groupsJson) ? groupsJson : null);
            var activeRole = RoleHelper.DetermineActiveRole(roles);

            var reservation = await _reservationService.GetReservationByIdAsync(cancelRequest.ReservationId);
            if (reservation is null)
                return ResponseBuilder.CreateErrorResponse(404, "Reservation not found", context.Logger);

            if (!IsUserAuthorizedToModifyReservation(activeRole, userId, reservation))
                return ResponseBuilder.CreateErrorResponse(403, "Not authorized to cancel", context.Logger);

            var timeRemaining = reservation.StartTime - DateTime.UtcNow;
            if (timeRemaining.TotalMinutes < 30)
                return ResponseBuilder.CreateErrorResponse(400, "Too late to cancel reservation. Must be 30 minutes before reservation time", context.Logger);

            reservation.Status = ReservationStatus.Cancelled;
            await _reservationService.UpdateReservationAsync(reservation);

            var waiter = await _waiterService.FindWaiterByReservationIdAsync(reservation.Id);
            if (waiter != null)
            {
                var updatedList = waiter.Reservations.Where(id => id != reservation.Id);
                await _waiterService.UpdateWaiterReservationsAsync(waiter.UserId, updatedList);
            }

            return ResponseBuilder.CreateSuccessResponse("Reservation cancelled successfully");
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid JSON format", context.Logger, ex);
        }
        catch (AmazonDynamoDBException ex)
        {
            return ResponseBuilder.CreateErrorResponse(503, "Database error occurred", context.Logger, ex);
        }
    }

    private static CancelReservationRequest ParseRequestBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ValidationException("Request body is empty");
        }

        var result = JsonSerializer.Deserialize<CancelReservationRequest>(body)
            ?? throw new ValidationException("Unable to parse request body");

        return result;
    }

    private static bool IsUserAuthorizedToModifyReservation(string activeRole, string userId, Reservation reservation)
    {
        if (activeRole == Roles.Waiter.ToString())
            return reservation.WaiterId == userId;

        if (activeRole == Roles.Customer.ToString())
            return reservation.CustomerId == userId;

        return false;
    }
}