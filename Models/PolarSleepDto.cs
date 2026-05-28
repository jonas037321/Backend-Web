using System.Text.Json.Serialization;

namespace Models;

public class PolarSleepRootObject
{
    [JsonPropertyName("nights")]
    public List<PolarSleepNightDto> Nights { get; set; } = new();
}

public class PolarSleepNightDto
{
    [JsonPropertyName("polar_user")]
    public string PolarUser { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("sleep_start_time")]
    public string SleepStartTime { get; set; } = string.Empty;

    [JsonPropertyName("sleep_end_time")]
    public string SleepEndTime { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("continuity")]
    public double Continuity { get; set; }

    [JsonPropertyName("continuity_class")]
    public int ContinuityClass { get; set; }

    [JsonPropertyName("light_sleep")]
    public int LightSleep { get; set; }

    [JsonPropertyName("deep_sleep")]
    public int DeepSleep { get; set; }

    [JsonPropertyName("rem_sleep")]
    public int RemSleep { get; set; }

    [JsonPropertyName("unrecognized_sleep_stage")]
    public int UnrecognizedSleepStage { get; set; }

    [JsonPropertyName("sleep_score")]
    public int SleepScore { get; set; }

    [JsonPropertyName("total_interruption_duration")]
    public int TotalInterruptionDuration { get; set; }

    [JsonPropertyName("sleep_charge")]
    public int SleepCharge { get; set; }

    [JsonPropertyName("sleep_goal")]
    public int SleepGoal { get; set; }

    [JsonPropertyName("sleep_rating")]
    public int SleepRating { get; set; }

    [JsonPropertyName("short_interruption_duration")]
    public int ShortInterruptionDuration { get; set; }

    [JsonPropertyName("long_interruption_duration")]
    public int LongInterruptionDuration { get; set; }

    [JsonPropertyName("sleep_cycles")]
    public int SleepCycles { get; set; }

    [JsonPropertyName("group_duration_score")]
    public double GroupDurationScore { get; set; }

    [JsonPropertyName("group_solidity_score")]
    public double GroupSolidityScore { get; set; }

    [JsonPropertyName("group_regeneration_score")]
    public double GroupRegenerationScore { get; set; }

    [JsonPropertyName("hypnogram")]
    public Dictionary<string, int>? Hypnogram { get; set; }

    [JsonPropertyName("heart_rate_samples")]
    public Dictionary<string, int>? HeartRateSamples { get; set; }

    [JsonIgnore]
    public int? LatestHeartRate => HeartRateSamples is null || HeartRateSamples.Count == 0
        ? null
        : HeartRateSamples.Values.Max();

    [JsonIgnore]
    public int? AverageHeartRate => HeartRateSamples is null || HeartRateSamples.Count == 0
        ? null
        : (int)Math.Round(HeartRateSamples.Values.Average());
}
