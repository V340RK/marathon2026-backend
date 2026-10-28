using APIHandler.Constants.Dishes;
using APIHandler.Entities;
using APIHandler.Models.Dishes;
using System;

namespace APIHandler.Mappers;

public class DishMapper
{
    public static Dish FromCreateRequest(CreateDishRequest createDishRequest)
    {
        var dishId = Guid.NewGuid().ToString();

        return new Dish
        {
            Id = dishId,
            Name = createDishRequest.Name,
            Description = createDishRequest.Description,
            DishType = createDishRequest.DishType,
            Price = createDishRequest.Price,
            Weight = createDishRequest.Weight,
            Calories = createDishRequest.Calories,
            Proteins = createDishRequest.Proteins,
            Fats = createDishRequest.Fats,
            Carbohydrates = createDishRequest.Carbohydrates,
            Vitamins = createDishRequest.Vitamins,
            State = createDishRequest.State,
        };
    }

    public static DishListResponse ToDishListResponse(Dish dish)
    {
        return new DishListResponse
        {
            Id = dish.Id,
            Name = dish.Name,
            ImageUrl = string.IsNullOrWhiteSpace(dish.ImageUrl) ? null : dish.ImageUrl,
            Price = dish.Price,
            State = FormatDishState(dish.State),
            Weight = dish.Weight,
            DishType = FormatDishType(dish.DishType),
            Description = dish.Description,
            Calories = dish.Calories,
            Carbohydrates = dish.Carbohydrates,
            Fats = dish.Fats,
            Proteins = dish.Proteins,
            Vitamins = dish.Vitamins,
        };
    }

    public static DishResponse ToDishResponse(Dish dish)
    {
        return new DishResponse
        {
            Id = dish.Id,
            Name = dish.Name,
            Description = dish.Description,
            DishType = FormatDishType(dish.DishType),
            Price = dish.Price,
            Weight = dish.Weight,
            Calories = dish.Calories,
            Proteins = dish.Proteins,
            Fats = dish.Fats,
            Carbohydrates = dish.Carbohydrates,
            Vitamins = dish.Vitamins,
            ImageUrl = string.IsNullOrWhiteSpace(dish.ImageUrl)
                ? null
                : dish.ImageUrl,
            State = FormatDishState(dish.State),
        };
    }

    private static string FormatDishState(DishState state)
    {
        return state switch
        {
            DishState.OnStop => DishStateLabels.OnStop,
            DishState.Available => DishStateLabels.Available,
            DishState.OutOfStock => DishStateLabels.OutOfStock,
            _ => "Unknown"
        };
    }

    private static string FormatDishType(DishType type)
    {
        return type switch
        {
            DishType.Appetizer => DishTypeLabels.Appetizer,
            DishType.MainCourse => DishTypeLabels.MainCourse,
            DishType.Dessert => DishTypeLabels.Dessert,
            _ => "Unknown"
        };
    }
}