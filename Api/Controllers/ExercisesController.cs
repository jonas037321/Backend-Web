using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly HttpClient _httpClient;

    private const string AccessToken = "19dca384d80e278f891252bca4d42a0c";
    private const string UserId = "48424957";
    private const string BaseUrl = "https://www.polaraccesslink.com/v3/users";

    public ExercisesController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("historical")]
    [ProducesResponseType(typeof(List<PolarExerciseDto>), StatusCodes.Status200OK)]
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
                return Ok(new List<PolarExerciseDto>());
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
                return Ok(new List<PolarExerciseDto>());
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

            return Ok(rootData?.Exercises ?? new List<PolarExerciseDto>());
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
