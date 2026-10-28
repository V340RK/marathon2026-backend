using System.Text.Json.Serialization;

namespace APIHandler.Entities;

// Who pays for a participant's dishes: the guest (Individual) or the reservation owner (Shared)
[JsonConverter(typeof(JsonStringEnumConverter<BillType>))]
public enum BillType
{
    Individual = 0,
    Shared = 1,
}
