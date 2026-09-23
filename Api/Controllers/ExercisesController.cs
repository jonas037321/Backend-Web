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

    private const string AccessToken = "19dca384d80e278f891252bca4d42a0c";
    private const string UserId = "48424957";
    private const string BaseUrl = "https://www.polaraccesslink.com/v3/users";
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

    [HttpGet("historical")]
    [ProducesResponseType(typeof(List<PolarDetailedExerciseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHistoricalExercises()
    {
        long? activeTransactionId = null;

        try
        {
            var startUrl = $"{BaseUrl}/{UserId}/exercise-transactions?resource=EXERCISE";
            var startResponse = await _httpClient.SendAsync(CreatePolarRequest(HttpMethod.Post, startUrl));

            if (startResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return Ok(new List<PolarDetailedExerciseDto>());
            }

            if (startResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new { error = "Es ist noch eine Transaktion offen. Bitte API-Konsole prüfen." });
            }

            if (!startResponse.IsSuccessStatusCode)
            {
                var err = await startResponse.Content.ReadAsStringAsync();
                return StatusCode((int)startResponse.StatusCode, new { error = "Transaktionsstart fehlgeschlagen", details = err });
            }

            var startJson = await startResponse.Content.ReadAsStringAsync();
            var transaction = JsonSerializer.Deserialize<PolarTransactionResponse>(startJson);

            if (transaction is null || transaction.TransactionId == 0)
            {
                return BadRequest(new { error = "Keine gültige Transaktions-ID von Polar erhalten." });
            }

            activeTransactionId = transaction.TransactionId;

            var listUrl = $"{BaseUrl}/{UserId}/exercise-transactions/{activeTransactionId}";
            var listResponse = await _httpClient.SendAsync(CreatePolarRequest(HttpMethod.Get, listUrl));

            if (listResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                await CloseTransactionAsync(activeTransactionId.Value);
                return Ok(new List<PolarDetailedExerciseDto>());
            }

            if (!listResponse.IsSuccessStatusCode)
            {
                var err = await listResponse.Content.ReadAsStringAsync();
                return StatusCode((int)listResponse.StatusCode, new { error = "Übungsdaten konnten nicht geladen werden.", details = err });
            }

            var listJson = await listResponse.Content.ReadAsStringAsync();
            var rootData = JsonSerializer.Deserialize<PolarExerciseRootObject>(listJson);

            await CloseTransactionAsync(activeTransactionId.Value);
            activeTransactionId = null;

            return Ok(rootData?.Exercises ?? new List<PolarDetailedExerciseDto>());
        }
        catch (Exception ex)
        {
            if (activeTransactionId.HasValue)
            {
                await CloseTransactionAsync(activeTransactionId.Value);
            }

            return StatusCode(500, new { error = "Interner Serverfehler", message = ex.Message });
        }
    }

    private static DateTime ParseStartTime(string value)
    {
        return DateTime.TryParse(value, out var parsedDate)
            ? parsedDate
            : DateTime.MinValue;
    }

    private async Task CloseTransactionAsync(long transactionId)
    {
        try
        {
            var closeUrl = $"{BaseUrl}/{UserId}/exercise-transactions/{transactionId}";
            await _httpClient.SendAsync(CreatePolarRequest(HttpMethod.Put, closeUrl));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Schließen der Transaktion: {ex.Message}");
        }
    }

    private static HttpRequestMessage CreatePolarRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        return request;
    }
}
