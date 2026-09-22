using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ORM;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PolarController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly DbManager _dbManager;

    // Deine echten Credentials aus dem Screenshot
    private const string ClientId = "1ba18f7c-cdc4-40ed-bbf6-4279a27f1d86";
    private const string ClientSecret = "4d43c44d-e769-4028-b143-0687c561372d";

    private const string PolarTokenUrl = "https://polarremote.com/v1/oauth/token";
    private const string RedirectUri = "https://localhost:7262/api/polar/callback";
    private const string FrontendUrl = "http://localhost:5173";

    public PolarController(IHttpClientFactory httpClientFactory, DbManager dbManager)
    {
        _httpClient = httpClientFactory.CreateClient();
        _dbManager = dbManager;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("E-Mail-Adresse fehlt.");

        var user = await _dbManager.FindUserByEmailAsync(email);
        if (user == null)
            return NotFound("User nicht gefunden");

        return Ok(new { connected = !string.IsNullOrEmpty(user.PolarAccessToken) });
    }

    [HttpGet("connect")]
    public IActionResult Connect([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("E-Mail-Adresse fehlt.");

        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
            ["state"] = email
        };

        // GEÄNDERT: auth.polar.com statt flow.polar.com
        var polarAuthUrl = QueryHelpers.AddQueryString("https://auth.polar.com/oauth2/authorize", query);
        return Redirect(polarAuthUrl);
    }

    // 2. Der offizielle Callback, den du bei Polar hinterlegt hast (https://localhost:7262/api/polar/callback)
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{FrontendUrl}/polar?error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest("Code oder State fehlt.");

        var email = state;

        // GEÄNDERT: v2 statt v1
        var request = new HttpRequestMessage(HttpMethod.Post, "https://polarremote.com/v2/oauth2/token");
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri
        });

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Token-Fehler: {body}");
            return Redirect($"{FrontendUrl}/polar?error=token_exchange_failed");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string accessToken = root.GetProperty("access_token").GetString()!;
        long polarUserId = root.GetProperty("x_user_id").GetInt64();

        var user = await _dbManager.FindUserByEmailAsync(email);
        if (user == null)
            return Redirect($"{FrontendUrl}/polar?error=user_not_found");

        user.PolarAccessToken = accessToken;
        user.PolarUserId = polarUserId.ToString();
        await _dbManager.UpdateUserAsync(user);

        await RegisterUserAtPolar(accessToken, polarUserId);

        return Redirect($"{FrontendUrl}/dashboard?polar=connected");
    }

    private async Task RegisterUserAtPolar(string accessToken, long polarUserId)
    {
        try
        {
            var registerRequest = new HttpRequestMessage(HttpMethod.Post, "https://www.polaraccesslink.com/v3/users");
            registerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var jsonPayload = JsonSerializer.Serialize(new { member_id = polarUserId.ToString() });
            registerRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var regResponse = await _httpClient.SendAsync(registerRequest);
            Console.WriteLine($"Polar Registrierungs-Status: {regResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler bei der Polar-User-Registrierung: {ex.Message}");
        }
    }
}