using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly DbManager _dbManager;

    private const string ExercisesUrl = "https://www.polaraccesslink.com/v3/exercises";

    public ExercisesController(
        IHttpClientFactory httpClientFactory,
        DbManager dbManager)
    {
        _httpClient = httpClientFactory.CreateClient();
        _dbManager = dbManager;
    }

    // Alle Trainings des letzten Monats (Polar liefert ohnehin nur Uploads der letzten 30 Tage)
    [HttpGet("{email}")]
    [ProducesResponseType(typeof(List<PolarDetailedExerciseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLastMonthExercises(string email)
    {
        try
        {
            var user = await _dbManager.FindUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User nicht gefunden");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, ExercisesUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.PolarAccessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new
                {
                    error = "Fehler beim Abruf der Trainings von Polar",
                    details = errorContent
                });
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var exercises = JsonSerializer.Deserialize<List<PolarDetailedExerciseDto>>(jsonString)
                ?? new List<PolarDetailedExerciseDto>();

            var oneMonthAgo = DateTime.Today.AddMonths(-1);

            var lastMonthExercises = exercises
                .Where(exercise => ParseStartTime(exercise.StartTime) >= oneMonthAgo)
                .OrderByDescending(exercise => ParseStartTime(exercise.StartTime))
                .ToList();

            return Ok(lastMonthExercises);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Interner Serverfehler", message = ex.Message });
        }
    }

    // Detailansicht einer Einheit: Kennzahlen, Pulszonen, Verläufe und Route
    [HttpGet("{email}/{exerciseId}")]
    [ProducesResponseType(typeof(ExerciseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExerciseDetail(string email, string exerciseId)
    {
        try
        {
            var user = await _dbManager.FindUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User nicht gefunden");
            }

            var detailUrl = $"{ExercisesUrl}/{Uri.EscapeDataString(exerciseId)}?samples=true&zones=true&route=true";
            var request = new HttpRequestMessage(HttpMethod.Get, detailUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.PolarAccessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new
                {
                    error = "Fehler beim Abruf der Trainingsdetails von Polar",
                    details = errorContent
                });
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var rawDetail = JsonSerializer.Deserialize<PolarExerciseRawDetailDto>(jsonString);

            if (rawDetail == null)
            {
                return NotFound("Training nicht gefunden");
            }

            return Ok(BuildExerciseDetail(rawDetail));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Interner Serverfehler", message = ex.Message });
        }
    }

    private const int MaxSeriesPoints = 400;
    private const int MaxRoutePoints = 600;
    // Höhenänderungen unter 3 m gelten als Messrauschen und zählen nicht zu den Höhenmetern
    private const double AltitudeNoiseThreshold = 3.0;

    private static ExerciseDetailDto BuildExerciseDetail(PolarExerciseRawDetailDto rawDetail)
    {
        var heartRates = GetSampleValues(rawDetail, 0);
        var speeds = GetSampleValues(rawDetail, 1);
        var cadences = GetSampleValues(rawDetail, 2);
        var altitudes = GetSampleValues(rawDetail, 3);
        var powers = GetSampleValues(rawDetail, 4);
        var temperatures = GetSampleValues(rawDetail, 9);
        var distances = GetSampleValues(rawDetail, 10);

        // Polar schreibt 0, solange noch kein Messwert vorliegt (z. B. vor dem ersten GPS- oder Barometer-Fix)
        var validAltitudes = altitudes.Where(value => value.HasValue && value != 0).Select(value => value!.Value).ToList();
        var (ascent, descent) = CalculateElevationChange(validAltitudes);

        var detail = new ExerciseDetailDto
        {
            Exercise = rawDetail,
            Ascent = validAltitudes.Count > 1 ? Math.Round(ascent) : null,
            Descent = validAltitudes.Count > 1 ? Math.Round(descent) : null,
            MinAltitude = validAltitudes.Count > 0 ? Math.Round(validAltitudes.Min()) : null,
            MaxAltitude = validAltitudes.Count > 0 ? Math.Round(validAltitudes.Max()) : null,
            AvgSpeed = Average(speeds),
            MaxSpeed = Maximum(speeds),
            AvgCadence = Average(cadences),
            MaxCadence = Maximum(cadences),
            AvgPower = Average(powers),
            MaxPower = Maximum(powers),
            AvgTemperature = Average(temperatures),
            HeartRateZones = (rawDetail.HeartRateZones ?? new List<PolarHeartRateZoneRawDto>())
                .Select(zone => new HeartRateZoneDto
                {
                    Index = zone.Index,
                    LowerLimit = zone.LowerLimit,
                    UpperLimit = zone.UpperLimit,
                    Seconds = (int)ParseDuration(zone.InZone).TotalSeconds
                })
                .ToList()
        };

        var sampleCount = new[] { heartRates.Count, altitudes.Count, speeds.Count, distances.Count }.Max();
        var seriesStep = Math.Max(1, (int)Math.Ceiling(sampleCount / (double)MaxSeriesPoints));
        var recordingRate = Math.Max(1, rawDetail.Samples?.FirstOrDefault()?.RecordingRate ?? 1);

        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex += seriesStep)
        {
            detail.Series.Add(new ExerciseSeriesPointDto
            {
                Seconds = sampleIndex * recordingRate,
                Distance = ValueAt(distances, sampleIndex),
                HeartRate = NonZero(ValueAt(heartRates, sampleIndex)),
                Altitude = NonZero(ValueAt(altitudes, sampleIndex)),
                Speed = ValueAt(speeds, sampleIndex)
            });
        }

        var route = rawDetail.Route ?? new List<PolarRoutePointRawDto>();
        var routeStep = Math.Max(1, (int)Math.Ceiling(route.Count / (double)MaxRoutePoints));
        for (var routeIndex = 0; routeIndex < route.Count; routeIndex += routeStep)
        {
            detail.Route.Add(new[] { route[routeIndex].Latitude, route[routeIndex].Longitude });
        }

        return detail;
    }

    private static List<double?> GetSampleValues(PolarExerciseRawDetailDto rawDetail, int sampleType)
    {
        var sample = rawDetail.Samples?.FirstOrDefault(item => item.SampleType == sampleType);
        if (sample == null || string.IsNullOrWhiteSpace(sample.Data))
            return new List<double?>();

        return sample.Data
            .Split(',')
            .Select(value => double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : (double?)null)
            .ToList();
    }

    // Hysterese: erst ab einer Änderung von mehr als AltitudeNoiseThreshold wird gezählt
    private static (double Ascent, double Descent) CalculateElevationChange(List<double> altitudes)
    {
        if (altitudes.Count < 2)
            return (0, 0);

        double ascent = 0, descent = 0;
        var referenceAltitude = altitudes[0];

        foreach (var altitude in altitudes)
        {
            var difference = altitude - referenceAltitude;
            if (difference >= AltitudeNoiseThreshold)
            {
                ascent += difference;
                referenceAltitude = altitude;
            }
            else if (difference <= -AltitudeNoiseThreshold)
            {
                descent -= difference;
                referenceAltitude = altitude;
            }
        }

        return (ascent, descent);
    }

    private static double? Average(List<double?> values)
    {
        var positiveValues = values.Where(value => value > 0).Select(value => value!.Value).ToList();
        return positiveValues.Count > 0 ? Math.Round(positiveValues.Average(), 1) : null;
    }

    private static double? Maximum(List<double?> values)
    {
        var positiveValues = values.Where(value => value > 0).Select(value => value!.Value).ToList();
        return positiveValues.Count > 0 ? Math.Round(positiveValues.Max(), 1) : null;
    }

    private static double? ValueAt(List<double?> values, int index) =>
        index < values.Count ? values[index] : null;

    private static double? NonZero(double? value) =>
        value is null or 0 ? null : Math.Round(value.Value, 1);

    private static TimeSpan ParseDuration(string value)
    {
        try
        {
            return System.Xml.XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException)
        {
            return TimeSpan.Zero;
        }
    }

    private static DateTime ParseStartTime(string value)
    {
        return DateTime.TryParse(value, out var parsedDate)
            ? parsedDate
            : DateTime.MinValue;
    }
}
