using APIHandler.Constants.Dishes;
using APIHandler.Models.Dishes;
using System.Collections.Generic;

namespace APIHandler.Models.GuestSessions;

public record GuestMenuResponse(Dictionary<DishType, IEnumerable<GuestMenuDish>> Dishes);