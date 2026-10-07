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

    private static DateTime ParseStartTime(string value)
    {
        return DateTime.TryParse(value, out var parsedDate)
            ? parsedDate
            : DateTime.MinValue;
    }
}
