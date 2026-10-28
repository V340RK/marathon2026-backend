using APIHandler.Models.WaiterList;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Waiters;

public interface IWaiterService
{
    Task AddWaiterToListAsync(UpdateWaiterListRequest request);

    Task UpdateWaiterReservationsAsync(string email, IEnumerable<string> reservationIds);

    Task RemoveWaiterFromListAsync(string email);

    Task<bool> IsWaiterInListAsync(string email);

    Task<WaiterListItem> GetLeastBusyWaiterAsync(string locationId);

    Task<string?> GetWaiterLocationIdAsync(string waiterId);

    Task<List<WaiterListItem>> GetWaitersByLocationIdAsync(string locationId);

    Task<WaiterListItem?> GetWaiterByReservationIdAsync(string reservationId);

    Task<WaiterListItem> FindWaiterByIdAsync(string userId);

    Task<WaiterListItem?> FindWaiterByReservationIdAsync(string reservationId);

    Task RemoveWaiterByIdAsync(string userId);
}