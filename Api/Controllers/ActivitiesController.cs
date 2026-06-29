using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;
using System.Net.Http.Headers;
using System.Text.Json;
namespace Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly DbManager _dbManager;

    private const string PolarUrl = "https://www.polaraccesslink.com/v3/users/activities";

    public ActivitiesController(
        IHttpClientFactory httpClientFactory,
        DbManager dbManager)
    {
        _httpClient = httpClientFactory.CreateClient();
        _dbManager = dbManager;
    }

    [HttpGet("{email}")]
    public async Task<IActionResult> GetAllActivities(string email)
    {
        try
        {
            var user = await _dbManager.FindUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User nicht gefunden");
            }

            var accessToken = user.PolarAccessToken;
            var userId = user.PolarUserId; // falls später benötigt

            var request = new HttpRequestMessage(HttpMethod.Get, PolarUrl);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, new
                {
                    error = "Polar API hat einen Fehler gemeldet",
                    details = errorContent
                });
            }

            var jsonString = await response.Content.ReadAsStringAsync();

            var activities =
                JsonSerializer.Deserialize<List<PolarActivityDto>>(jsonString);

            return Ok(activities);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Interner Serverfehler",
                message = ex.Message
            });
        }
    }
}