using System.Globalization;
using System.Xml;
using Models;

namespace Api.Services;

// Rechnet die Rohdaten eines Trainings (Sekunden-Samples, Zonen, Route) in kompakte Kennzahlen und Verläufe um
public static class ExerciseDetailBuilder
{
    private const int MaxSeriesPoints = 400;
    private const int MaxRoutePoints = 600;
    // Höhenänderungen unter 3 m gelten als Messrauschen und zählen nicht zu den Höhenmetern
    private const double AltitudeNoiseThreshold = 3.0;

    // Sample-Typen laut Polar-Doku
    private const int HeartRateSample = 0, SpeedSample = 1, CadenceSample = 2, AltitudeSample = 3,
        PowerSample = 4, TemperatureSample = 9, DistanceSample = 10;

    public static ExerciseDetailDto Build(PolarExerciseRawDetailDto exercise)
    {
        var heartRates = GetSamples(exercise, HeartRateSample);
        var speeds = GetSamples(exercise, SpeedSample);
        var cadences = GetSamples(exercise, CadenceSample);
        var altitudes = GetSamples(exercise, AltitudeSample);
        var powers = GetSamples(exercise, PowerSample);
        var temperatures = GetSamples(exercise, TemperatureSample);
        var distances = GetSamples(exercise, DistanceSample);

        // Polar schreibt 0, solange noch kein Messwert vorliegt (z. B. vor dem ersten GPS- oder Barometer-Fix)
        var validAltitudes = altitudes.Where(altitude => altitude > 0).ToList();
        var (ascent, descent) = CalculateElevationChange(validAltitudes);
        var hasAltitude = validAltitudes.Count > 1;

        var sampleCount = new[] { heartRates.Count, altitudes.Count, speeds.Count, distances.Count }.Max();
        var recordingRate = Math.Max(1, exercise.Samples?.FirstOrDefault()?.RecordingRate ?? 1);
        var route = exercise.Route ?? [];

        return new ExerciseDetailDto
        {
            Exercise = exercise,
            Ascent = hasAltitude ? Math.Round(ascent) : null,
            Descent = hasAltitude ? Math.Round(descent) : null,
            MinAltitude = hasAltitude ? Math.Round(validAltitudes.Min()) : null,
            MaxAltitude = hasAltitude ? Math.Round(validAltitudes.Max()) : null,
            AvgSpeed = Average(speeds),
            MaxSpeed = Maximum(speeds),
            AvgCadence = Average(cadences),
            MaxCadence = Maximum(cadences),
            AvgPower = Average(powers),
            MaxPower = Maximum(powers),
            AvgTemperature = Average(temperatures),
            HeartRateZones = (exercise.HeartRateZones ?? [])
                .Select(zone => new HeartRateZoneDto
                {
                    Index = zone.Index,
                    LowerLimit = zone.LowerLimit,
                    UpperLimit = zone.UpperLimit,
                    Seconds = (int)ParseDuration(zone.InZone).TotalSeconds
                })
                .ToList(),
            Series = EveryNth(Enumerable.Range(0, sampleCount).ToList(), MaxSeriesPoints)
                .Select(index => new ExerciseSeriesPointDto
                {
                    Seconds = index * recordingRate,
                    Distance = ValueAt(distances, index),
                    HeartRate = PositiveOrNull(ValueAt(heartRates, index)),
                    Altitude = PositiveOrNull(ValueAt(altitudes, index)),
                    Speed = ValueAt(speeds, index)
                })
                .ToList(),
            Route = EveryNth(route, MaxRoutePoints)
                .Select(point => new[] { point.Latitude, point.Longitude })
                .ToList()
        };
    }

    // "82,82,81,..." -> Liste von Zahlen
    private static List<double> GetSamples(PolarExerciseRawDetailDto exercise, int sampleType)
    {
        var data = exercise.Samples?.FirstOrDefault(sample => sample.SampleType == sampleType)?.Data;
        if (string.IsNullOrWhiteSpace(data))
            return [];

        return data.Split(',')
            .Select(value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0)
            .ToList();
    }

    // Hysterese: erst ab einer Änderung von mindestens AltitudeNoiseThreshold wird gezählt
    private static (double Ascent, double Descent) CalculateElevationChange(List<double> altitudes)
    {
        double ascent = 0, descent = 0;
        var referenceAltitude = altitudes.FirstOrDefault();

        foreach (var altitude in altitudes)
        {
            var difference = altitude - referenceAltitude;
            if (Math.Abs(difference) < AltitudeNoiseThreshold)
                continue;

            if (difference > 0) ascent += difference;
            else descent -= difference;
            referenceAltitude = altitude;
        }

        return (ascent, descent);
    }

    // Verkleinert eine Liste auf höchstens maxCount gleichmäßig verteilte Einträge
    private static IEnumerable<T> EveryNth<T>(List<T> items, int maxCount)
    {
        var step = Math.Max(1, (int)Math.Ceiling(items.Count / (double)maxCount));
        return items.Where((_, index) => index % step == 0);
    }

    private static double? Average(List<double> values) =>
        values.Any(value => value > 0) ? Math.Round(values.Where(value => value > 0).Average(), 1) : null;

    private static double? Maximum(List<double> values) =>
        values.Any(value => value > 0) ? Math.Round(values.Max(), 1) : null;

    private static double? ValueAt(List<double> values, int index) =>
        index < values.Count ? values[index] : null;

    private static double? PositiveOrNull(double? value) =>
        value > 0 ? Math.Round(value.Value, 1) : null;

    private static TimeSpan ParseDuration(string value)
    {
        try
        {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException)
        {
            return TimeSpan.Zero;
        }
    }
}
