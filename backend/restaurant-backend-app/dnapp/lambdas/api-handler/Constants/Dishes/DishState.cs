using System.Text.Json.Serialization;

namespace APIHandler.Constants.Dishes;

[JsonConverter(typeof(JsonStringEnumConverter<DishState>))]
public enum DishState
{
    OnStop = 0,
    Available = 1,
    OutOfStock = 2
}