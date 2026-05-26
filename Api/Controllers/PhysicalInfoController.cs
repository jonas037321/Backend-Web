using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhysicalInfoController : ControllerBase
{
    private readonly HttpClient _httpClient;

    private const string AccessToken = "19dca384d80e278f891252bca4d42a0c";
    private const string PolarUrl = "https://www.polaraccesslink.com/v3/users/physical-info";

    public PhysicalInfoController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet]
    public async Task<IActionResult> GetPhysicalInfo()
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
                return StatusCode((int)response.StatusCode, new
                {
                    error = "Fehler beim Abruf der körperlichen Daten von Polar",
                    details = errorContent
                });
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var physicalInfo = JsonSerializer.Deserialize<PolarPhysicalInfoDto>(jsonString);

            return Ok(physicalInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Interner Serverfehler", message = ex.Message });
        }
    }
}
