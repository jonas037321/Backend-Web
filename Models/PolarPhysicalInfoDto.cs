using System.Text.Json.Serialization;

namespace Models;

public class PolarPhysicalInfoDto
{
    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

    [JsonPropertyName("modified")]
    public string Modified { get; set; } = string.Empty;

    [JsonPropertyName("birthday")]
    public string Birthday { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("maximum_heart_rate")]
    public int MaximumHeartRate { get; set; }

    [JsonPropertyName("resting_heart_rate")]
    public int RestingHeartRate { get; set; }

    [JsonPropertyName("aerobic_threshold")]
    public int AerobicThreshold { get; set; }

    [JsonPropertyName("anaerobic_threshold")]
    public int AnaerobicThreshold { get; set; }

    [JsonPropertyName("vo2_max")]
    public int Vo2Max { get; set; }

    [JsonPropertyName("weight_source")]
    public string WeightSource { get; set; } = string.Empty;

    [JsonPropertyName("training_background")]
    public string TrainingBackground { get; set; } = string.Empty;

    [JsonPropertyName("typical_day")]
    public string TypicalDay { get; set; } = string.Empty;

    [JsonPropertyName("sleep_goal")]
    public string SleepGoal { get; set; } = string.Empty;
}
