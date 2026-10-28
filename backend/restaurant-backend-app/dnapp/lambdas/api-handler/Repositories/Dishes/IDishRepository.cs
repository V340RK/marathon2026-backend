using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Dishes;

public interface IDishRepository
{
    Task<Dish?> GetDishByIdAsync(string dishId);
    Task<IEnumerable<Dish>> GetDishesByIdsAsync(IEnumerable<string> dishIds);
    Task<IEnumerable<Dish>> GetDishesByLocationIdAsync(string locationId);
    Task SaveDishAsync(Dish dish);
    Task DeleteDishAsync(Dish dish);
}