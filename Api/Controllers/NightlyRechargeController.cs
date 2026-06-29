using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NightlyRechargeController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly DbManager _dbManager; // 1. DbManager hinzugefügt

    private const string PolarUrl = "https://www.polaraccesslink.com/v3/users/nightly-recharge";

    // 2. DbManager über den Konstruktor injizieren
    public NightlyRechargeController(
        IHttpClientFactory httpClientFactory,
        DbManager dbManager)
    {
        _httpClient = httpClientFactory.CreateClient();
        _dbManager = dbManager;
    }

    // 3. Route auf "{email}" angepasst
    [HttpGet("{email}")]
    [ProducesResponseType(typeof(List<PolarNightlyRechargeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNightlyRecharge(string email)
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
                    error = "Fehler beim Abruf der Nightly Recharge Daten von Polar",
                    details = errorContent
                });
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var root = JsonSerializer.Deserialize<Dictionary<string, List<PolarNightlyRechargeDto>>>(jsonString);

            return Ok(root != null && root.ContainsKey("recharges") ? root["recharges"] : new List<PolarNightlyRechargeDto>());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Interner Serverfehler", message = ex.Message });
        }
    }
}