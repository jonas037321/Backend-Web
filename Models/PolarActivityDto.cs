using System.Text.Json.Serialization;

namespace Models;

public class PolarActivityDto
{
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("active_duration")]
    public string ActiveDuration { get; set; } = string.Empty;

    [JsonPropertyName("calories")]
    public int Calories { get; set; }

    [JsonPropertyName("active_calories")]
    public int ActiveCalories { get; set; }

    [JsonPropertyName("steps")]
    public int Steps { get; set; }

    [JsonPropertyName("distance_from_steps")]
    public double DistanceFromSteps { get; set; }
}
