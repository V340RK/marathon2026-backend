using System.Text.Json.Serialization;

namespace APIHandler.Constants.Dishes;

[JsonConverter(typeof(JsonStringEnumConverter<DishType>))]
public enum DishType
{
    Appetizer = 0,
    MainCourse = 1,
    Dessert = 2,
}