using System.Text.Json.Serialization;

namespace Models;

// Rohantwort von GET /v3/exercises/{id}?samples=true&zones=true&route=true
public class PolarExerciseRawDetailDto : PolarDetailedExerciseDto
{
    [JsonPropertyName("heart_rate_zones")]
    public List<PolarHeartRateZoneRawDto>? HeartRateZones { get; set; }

    [JsonPropertyName("samples")]
    public List<PolarSampleRawDto>? Samples { get; set; }

    [JsonPropertyName("route")]
    public List<PolarRoutePointRawDto>? Route { get; set; }
}

public class PolarHeartRateZoneRawDto
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("lower_limit")]
    public int LowerLimit { get; set; }

    [JsonPropertyName("upper_limit")]
    public int UpperLimit { get; set; }

    [JsonPropertyName("in_zone")]
    public string InZone { get; set; } = string.Empty;
}

public class PolarSampleRawDto
{
    // 0 = Puls, 1 = Geschwindigkeit, 2 = Kadenz, 3 = Höhe, 4 = Leistung, 9 = Temperatur, 10 = Distanz
    [JsonPropertyName("sample_type")]
    public int SampleType { get; set; }

    [JsonPropertyName("recording_rate")]
    public int RecordingRate { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public class PolarRoutePointRawDto
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

// Aufbereitete Antwort für das Frontend (Kennzahlen + verkleinerte Verläufe)
public class ExerciseDetailDto
{
    [JsonPropertyName("exercise")]
    public PolarDetailedExerciseDto Exercise { get; set; } = new();

    [JsonPropertyName("ascent")]
    public double? Ascent { get; set; }

    [JsonPropertyName("descent")]
    public double? Descent { get; set; }

    [JsonPropertyName("min_altitude")]
    public double? MinAltitude { get; set; }

    [JsonPropertyName("max_altitude")]
    public double? MaxAltitude { get; set; }

    [JsonPropertyName("avg_speed")]
    public double? AvgSpeed { get; set; }

    [JsonPropertyName("max_speed")]
    public double? MaxSpeed { get; set; }

    [JsonPropertyName("avg_cadence")]
    public double? AvgCadence { get; set; }

    [JsonPropertyName("max_cadence")]
    public double? MaxCadence { get; set; }

    [JsonPropertyName("avg_power")]
    public double? AvgPower { get; set; }

    [JsonPropertyName("max_power")]
    public double? MaxPower { get; set; }

    [JsonPropertyName("avg_temperature")]
    public double? AvgTemperature { get; set; }

    [JsonPropertyName("heart_rate_zones")]
    public List<HeartRateZoneDto> HeartRateZones { get; set; } = new();

    [JsonPropertyName("series")]
    public List<ExerciseSeriesPointDto> Series { get; set; } = new();

    [JsonPropertyName("route")]
    public List<double[]> Route { get; set; } = new();
}

public class HeartRateZoneDto
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("lower_limit")]
    public int LowerLimit { get; set; }

    [JsonPropertyName("upper_limit")]
    public int UpperLimit { get; set; }

    [JsonPropertyName("seconds")]
    public int Seconds { get; set; }
}

public class ExerciseSeriesPointDto
{
    [JsonPropertyName("seconds")]
    public int Seconds { get; set; }

    [JsonPropertyName("distance")]
    public double? Distance { get; set; }

    [JsonPropertyName("heart_rate")]
    public double? HeartRate { get; set; }

    [JsonPropertyName("altitude")]
    public double? Altitude { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }
}
