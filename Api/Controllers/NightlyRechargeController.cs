using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NightlyRechargeController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private const string AccessToken = "19dca384d80e278f891252bca4d42a0c";
    private const string PolarUrl = "https://www.polaraccesslink.com/v3/users/nightly-recharge";

    public NightlyRechargeController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PolarNightlyRechargeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNightlyRecharge()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, PolarUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error = "Polar API Fehler", details = errorContent });
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
