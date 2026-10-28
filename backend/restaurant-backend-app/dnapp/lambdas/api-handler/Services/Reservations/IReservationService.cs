using Amazon.Lambda.Core;
using APIHandler.Entities;
using APIHandler.Models.Reservations;
using System.Threading.Tasks;

namespace APIHandler.Services.Reservations;

public interface IReservationService
{
    Task<string> GetLocationIdForReservation(string reservationId);
    Task<Reservation?> GetReservationByIdAsync(string reservationId);
    Task<bool> UpdateReservationAsync(Reservation reservation);

    Task<ReservationResponse> UpdateReservationTableAsync(string callerId, string reservationId, string newTableId, ILambdaLogger logger);

    Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, string callerId, bool isWaiter, ILambdaLogger logger);
}