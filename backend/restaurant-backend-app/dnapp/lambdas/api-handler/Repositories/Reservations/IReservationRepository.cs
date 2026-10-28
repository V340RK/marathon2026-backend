using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Reservations;

public interface IReservationRepository
{
    Task<Reservation?> GetReservationByIdAsync(string reservationId);
    Task<List<Reservation>> GetReservationsByTableIdAsync(string tableId);
    Task<IEnumerable<Reservation>> GetReservationsByWaiterIdAsync(string waiterId);
    Task<IEnumerable<Reservation>> GetReservationsByCustomerIdAsync(string userId);
    Task SaveReservationAsync(Reservation reservation);
}
