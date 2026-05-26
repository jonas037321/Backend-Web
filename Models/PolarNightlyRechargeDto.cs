using System.Text.Json.Serialization;

namespace Models;

public class PolarNightlyRechargeDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("heart_rate_avg")]
    public int HeartRateAvg { get; set; }

    [JsonPropertyName("beat_to_beat_interval_avg")]
    public int BeatToBeatIntervalAvg { get; set; }

    [JsonPropertyName("heart_rate_variability_avg")]
    public int HeartRateVariabilityAvg { get; set; }

    [JsonPropertyName("breathing_rate_avg")]
    public double BreathingRateAvg { get; set; }

    [JsonPropertyName("nightly_recharge_status")]
    public int NightlyRechargeStatus { get; set; }

    [JsonPropertyName("ans_charge_status")]
    public int AnsChargeStatus { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }
}
