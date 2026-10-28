using APIHandler.Entities;
using APIHandler.Models.Dishes;
using System.Collections.Generic;
using System.Linq;

namespace APIHandler.Mappers;

public static class GuestMenuDishMapper
{
    public static GuestMenuDish Map(Dish dish) => new GuestMenuDish
    {
        Id = dish.Id,
        Name = dish.Name,
        Description = dish.Description,
        Price = dish.Price,
        Weight = dish.Weight,
        Calories = dish.Calories,
        Proteins = dish.Proteins,
        Fats = dish.Fats,
        Carbohydrates = dish.Carbohydrates,
        Vitamins = dish.Vitamins,
        ImageUrl = dish.ImageUrl,
        State = dish.State,
    };

    public static IEnumerable<GuestMenuDish> Map(IEnumerable<Dish> dishes) => dishes.Select(Map).ToArray();
}
