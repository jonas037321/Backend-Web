using System.Text.Json;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;

namespace Api.Controllers;

// Alle Daten-Endpoints: holen die Daten des Users mit seinem gespeicherten Token von Polar
[ApiController]
[Route("api")]
public class PolarDataController(DbManager dbManager, PolarClient polarClient) : ControllerBase
{
    [HttpGet("activities/{email}")]
    public Task<IActionResult> GetActivities(string email) =>
        FetchFromPolarAsync<List<PolarActivityDto>>(email, "users/activities", activities => activities ?? []);

    [HttpGet("physicalinfo/{email}")]
    public Task<IActionResult> GetPhysicalInfo(string email) =>
        FetchFromPolarAsync<PolarPhysicalInfoDto>(email, "users/physical-info", physicalInfo => physicalInfo);

    // Die letzten 5 Nächte, neueste zuerst
    [HttpGet("sleep/{email}")]
    public Task<IActionResult> GetSleep(string email) =>
        FetchFromPolarAsync<PolarSleepRootObject>(email, "users/sleep", sleep =>
            (sleep?.Nights ?? []).OrderByDescending(night => night.Date).Take(5));

    [HttpGet("nightlyrecharge/{email}")]
    public Task<IActionResult> GetNightlyRecharge(string email) =>
        FetchFromPolarAsync<PolarNightlyRechargeRootObject>(email, "users/nightly-recharge", recharge => recharge?.Recharges ?? []);

    // Polar liefert nur Trainings der letzten 30 Tage, neueste zuerst
    [HttpGet("exercises/{email}")]
    public Task<IActionResult> GetExercises(string email) =>
        FetchFromPolarAsync<List<PolarDetailedExerciseDto>>(email, "exercises", exercises =>
            (exercises ?? []).OrderByDescending(exercise => exercise.StartTime));

    [HttpGet("exercises/{email}/{exerciseId}")]
    public Task<IActionResult> GetExerciseDetail(string email, string exerciseId) =>
        FetchFromPolarAsync<PolarExerciseRawDetailDto>(
            email,
            $"exercises/{Uri.EscapeDataString(exerciseId)}?samples=true&zones=true&route=true",
            exercise => exercise == null ? null : ExerciseDetailBuilder.Build(exercise));

    private async Task<IActionResult> FetchFromPolarAsync<T>(string email, string path, Func<T?, object?> selectResult)
    {
        var user = await dbManager.FindUserByEmailAsync(email);
        if (user?.PolarAccessToken == null)
            return NotFound("User nicht gefunden oder nicht mit Polar verbunden.");

        var response = await polarClient.SendAsync(HttpMethod.Get, path, user.PolarAccessToken);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, new { error = "Polar API hat einen Fehler gemeldet", details = body });

        // Polar antwortet mit 204 und leerem Body, wenn es keine Daten gibt
        var data = string.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize<T>(body);
        return Ok(selectResult(data));
    }
}
