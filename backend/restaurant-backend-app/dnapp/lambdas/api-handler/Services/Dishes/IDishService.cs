using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Dishes;

public interface IDishService
{
    Task<Dish> GetDishByIdAsync(string dishId);

    Task<IEnumerable<Dish>> GetDishesByIdsAsync(IEnumerable<string> dishIds);

    Task<Dish> UpdateDishAsync(string dishId, Models.Dishes.CreateDishRequest request, string? newImageUrl);

    Task DeleteDishAsync(string dishId, bool force = false);
}