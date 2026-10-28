using System;
using System.Text.Json.Serialization;

namespace APIHandler.Models.TimeSlots;

public class TimeSlotResponse
{
    [JsonPropertyName("start")]
    public DateTime Start { get; set; }

    [JsonPropertyName("end")]
    public DateTime End { get; set; }
}