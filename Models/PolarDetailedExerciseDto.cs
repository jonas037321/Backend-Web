using System.Text.Json.Serialization;

namespace Models;

public class PolarDetailedExerciseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("upload_time")]
    public string UploadTime { get; set; } = string.Empty;

    [JsonPropertyName("polar_user")]
    public string PolarUser { get; set; } = string.Empty;

    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("start_time_utc_offset")]
    public int StartTimeUtcOffset { get; set; }

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("heart_rate")]
    public PolarHeartRateDto? HeartRate { get; set; }

    [JsonPropertyName("sport")]
    public string Sport { get; set; } = string.Empty;

    [JsonPropertyName("has_route")]
    public bool HasRoute { get; set; }

    [JsonPropertyName("detailed_sport_info")]
    public string DetailedSportInfo { get; set; } = string.Empty;

    [JsonPropertyName("calories")]
    public int Calories { get; set; }

    [JsonPropertyName("fat_percentage")]
    public int FatPercentage { get; set; }

    [JsonPropertyName("carbohydrate_percentage")]
    public int CarbohydratePercentage { get; set; }

    [JsonPropertyName("protein_percentage")]
    public int ProteinPercentage { get; set; }

    [JsonPropertyName("training_load_pro")]
    public PolarTrainingLoadDto? TrainingLoadPro { get; set; }
}

public class PolarHeartRateDto
{
    [JsonPropertyName("average")]
    public int Average { get; set; }

    [JsonPropertyName("maximum")]
    public int Maximum { get; set; }
}

public class PolarTrainingLoadDto
{
    [JsonPropertyName("cardio-load")]
    public double CardioLoad { get; set; }

    [JsonPropertyName("cardio-load-interpretation")]
    public string CardioLoadInterpretation { get; set; } = string.Empty;

    [JsonPropertyName("muscle-load")]
    public double MuscleLoad { get; set; }

    [JsonPropertyName("muscle-load-interpretation")]
    public string MuscleLoadInterpretation { get; set; } = string.Empty;

    [JsonPropertyName("perceived-load")]
    public double PerceivedLoad { get; set; }

    [JsonPropertyName("perceived-load-interpretation")]
    public string PerceivedLoadInterpretation { get; set; } = string.Empty;

    [JsonPropertyName("user-rpe")]
    public string UserRpe { get; set; } = string.Empty;
}
