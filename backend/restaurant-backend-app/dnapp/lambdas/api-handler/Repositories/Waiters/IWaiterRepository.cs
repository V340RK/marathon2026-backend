using APIHandler.Models.WaiterList;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Waiters;

public interface IWaiterRepository
{
    Task<WaiterListItem?> GetWaiterByIdAsync(string userId);
    Task<List<WaiterListItem>> GetWaitersByLocationIdAsync(string locationId);
    Task<List<WaiterListItem>> GetAllWaitersAsync();
    Task DeleteWaiterAsync(WaiterListItem waiter);
    Task SaveWaiterAsync(WaiterListItem waiter);
}