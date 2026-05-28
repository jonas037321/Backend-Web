using System.Text.Json.Serialization;

namespace Models;

public class PolarExerciseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("upload_time")]
    public string UploadTime { get; set; } = string.Empty;

    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;

    [JsonPropertyName("sport")]
    public string Sport { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("calories")]
    public int Calories { get; set; }
}

public class PolarTransactionResponse
{
    [JsonPropertyName("transaction-id")]
    public long TransactionId { get; set; }

    [JsonPropertyName("resource-uri")]
    public string ResourceUri { get; set; } = string.Empty;
}

public class PolarExerciseRootObject
{
    [JsonPropertyName("exercises")]
    public List<PolarDetailedExerciseDto> Exercises { get; set; } = new();
}
