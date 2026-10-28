using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Models.Reservations;
using APIHandler.Records;
using System;

namespace APIHandler.Mappers;

public static class ReservationMapper
{
    public static Reservation ToReservation(this CreateReservationRequest request, ReservationContext context)
    => new()
    {
        Id = Guid.NewGuid().ToString(),
        TableId = request.TableId,
        StartTime = request.StartTime,
        EndTime = request.EndTime,
        GuestCount = request.GuestCount,
        CustomerId = context.CustomerId,
        WaiterId = context.WaiterId!,
        CreatedByWaiter = context.CreatedByWaiter,
        Status = ReservationStatus.Reserved
    };


    public static ReservationResponse ToReservationResponse(Reservation reservation, string address, Order? order = null, string restaurantName = "Green & Tasty", bool guestInviteLinkAvailable = false)
    => new()
    {
        Id = reservation.Id,
        TableId = reservation.TableId,
        StartTime = reservation.StartTime,
        EndTime = reservation.EndTime,
        GuestCount = reservation.GuestCount,
        Status = reservation.Status,
        WaiterId = reservation.WaiterId,
        Address = address,
        RestaurantName = restaurantName,
        CustomerId = reservation.CustomerId, // for waiter dashboard
        HasSubmittedPreOrder = order is not null && order.Status == OrderStatus.Submitted,
        GuestInviteLinkAvailable = guestInviteLinkAvailable,
    };
}