using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SleepController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly DbManager _dbManager; // 1. DbManager hinzugefügt

    private const string PolarUrl = "https://www.polaraccesslink.com/v3/users/sleep";

    // 2. DbManager über den Konstruktor injizieren
    public SleepController(
        IHttpClientFactory httpClientFactory,
        DbManager dbManager)
    {
        _httpClient = httpClientFactory.CreateClient();
        _dbManager = dbManager;
    }

    // 3. E-Mail als Pfad-Parameter hinzugefügt und 404-Response deklariert
    [HttpGet("{email}")]
    [ProducesResponseType(typeof(List<PolarSleepNightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSleepData(string email)
    {
        try
        {
            // 4. User anhand der E-Mail aus der Datenbank suchen
            var user = await _dbManager.FindUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User nicht gefunden");
            }

            // 5. Dynamisches Access Token aus dem User-Objekt laden
            var accessToken = user.PolarAccessToken;

            // 6. Request an Polar mit dem dynamischen Token aufbauen
            var request = new HttpRequestMessage(HttpMethod.Get, PolarUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new
                {
                    error = "Fehler beim Abruf der Schlafdaten von Polar",
                    details = errorContent
                });
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var rootData = JsonSerializer.Deserialize<PolarSleepRootObject>(jsonString);

            var latestFiveNights = (rootData?.Nights ?? new List<PolarSleepNightDto>())
                .OrderByDescending(night => ParseNightDate(night.Date))
                .Take(5)
                .ToList();

            return Ok(latestFiveNights);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Interner Serverfehler", message = ex.Message });
        }
    }

    private static DateTime ParseNightDate(string value)
    {
        return DateTime.TryParse(value, out var parsedDate)
            ? parsedDate
            : DateTime.MinValue;
    }
}