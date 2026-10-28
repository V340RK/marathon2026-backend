using APIHandler.Constants.Dishes;

namespace APIHandler.Models.Dishes;

public class GuestMenuDish
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public decimal Price { get; set; }

    public required string Weight { get; set; }

    public required string Calories { get; set; }

    public required string Proteins { get; set; }

    public required string Fats { get; set; }

    public required string Carbohydrates { get; set; }

    public required string Vitamins { get; set; }

    public required string? ImageUrl { get; set; }

    public required DishState State { get; set; }

    public bool Selectable => State == DishState.Available;
}
