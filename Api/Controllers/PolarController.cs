using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ORM;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PolarController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly DbManager _dbManager;

    // Credentials aus den .NET User-Secrets (dotnet user-secrets set "Polar:ClientSecret" "...")
    private readonly string _clientId;
    private readonly string _clientSecret;

    private const string RedirectUri = "https://localhost:7262/api/polar/callback";
    private const string FrontendUrl = "http://localhost:5173";

    // V4-Endpoints laut https://auth.polar.com/.well-known/openid-configuration
    // (der alte V3-Endpoint flow.polar.com/oauth2/authorization zeigt nur eine Fehlerseite)
    private const string AuthorizationUrl = "https://auth.polar.com/oauth/authorize";
    private const string TokenUrl = "https://auth.polar.com/oauth/token";

    public PolarController(IHttpClientFactory httpClientFactory, DbManager dbManager, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _dbManager = dbManager;
        _clientId = configuration["Polar:ClientId"]
            ?? throw new InvalidOperationException("Polar:ClientId fehlt in den User-Secrets.");
        _clientSecret = configuration["Polar:ClientSecret"]
            ?? throw new InvalidOperationException("Polar:ClientSecret fehlt in den User-Secrets.");
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
            ["client_id"] = _clientId,
            ["redirect_uri"] = RedirectUri,
            ["scope"] = "accesslink.read_all",
            // E-Mail Base64Url-kodieren, damit keine Sonderzeichen (@, .) in der URL landen
            ["state"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(email))
        };

        var polarAuthUrl = QueryHelpers.AddQueryString(AuthorizationUrl, query);
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

        string email;
        try
        {
            email = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(state));
        }
        catch (FormatException)
        {
            return BadRequest("Ungültiger State.");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
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
            Console.WriteLine($"Token-Fehler ({(int)response.StatusCode}): {body}");
            return Redirect($"{FrontendUrl}/polar?error=token_exchange_failed");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string accessToken = root.GetProperty("access_token").GetString()!;

        // x_user_id kommt bei V3 als Zahl; bei V4 ist das Format nicht dokumentiert
        if (!root.TryGetProperty("x_user_id", out var userIdElement) ||
            !long.TryParse(userIdElement.ToString(), out long polarUserId))
        {
            Console.WriteLine($"Token-Antwort ohne gültige x_user_id: {body}");
            return Redirect($"{FrontendUrl}/polar?error=missing_user_id");
        }

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