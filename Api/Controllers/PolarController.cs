using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ORM;

namespace Api.Controllers;

// OAuth-Verbindung zwischen einem User und seinem Polar-Konto (V3-Flow)
[ApiController]
[Route("api/polar")]
public class PolarController(DbManager dbManager, PolarClient polarClient, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : ControllerBase
{
    private const string AuthorizationUrl = "https://flow.polar.com/oauth2/authorization";
    private const string TokenUrl = "https://polarremote.com/v2/oauth2/token";
    private const string RedirectUri = "https://localhost:7262/api/polar/callback";
    private const string FrontendUrl = "http://localhost:5173";

    // Aus den .NET User-Secrets: dotnet user-secrets set "Polar:ClientSecret" "..."
    private string ClientId => configuration["Polar:ClientId"] ?? throw new InvalidOperationException("Polar:ClientId fehlt in den User-Secrets.");
    private string ClientSecret => configuration["Polar:ClientSecret"] ?? throw new InvalidOperationException("Polar:ClientSecret fehlt in den User-Secrets.");

    [HttpGet("status")]
    public async Task<IActionResult> Status(string email)
    {
        var user = await dbManager.FindUserByEmailAsync(email);
        return user == null
            ? NotFound("User nicht gefunden")
            : Ok(new { connected = !string.IsNullOrEmpty(user.PolarAccessToken) });
    }

    // Leitet zum Polar-Login weiter; die E-Mail reist Base64Url-kodiert im state mit
    [HttpGet("connect")]
    public IActionResult Connect(string email)
    {
        var polarLoginUrl = QueryHelpers.AddQueryString(AuthorizationUrl, new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
            ["scope"] = "accesslink.read_all",
            ["state"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(email))
        });

        return Redirect(polarLoginUrl);
    }

    // Polar ruft nach dem Login hierher zurück: Code gegen Token tauschen, speichern und User bei AccessLink registrieren
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string? code, string? state, string? error)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect($"{FrontendUrl}/?polar=error");

        var email = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(state));
        var user = await dbManager.FindUserByEmailAsync(email);
        if (user == null)
            return Redirect($"{FrontendUrl}/?polar=error");

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = RedirectUri
            })
        };
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var tokenResponse = await httpClientFactory.CreateClient().SendAsync(tokenRequest);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Token-Fehler ({(int)tokenResponse.StatusCode}): {tokenBody}");
            return Redirect($"{FrontendUrl}/?polar=error");
        }

        using var tokenJson = JsonDocument.Parse(tokenBody);
        user.PolarAccessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
        user.PolarUserId = tokenJson.RootElement.GetProperty("x_user_id").ToString();
        await dbManager.SaveChangesAsync();

        // Ohne Registrierung liefert Polar keine Daten (403). 409 = schon registriert, auch ok.
        var registerResponse = await polarClient.SendAsync(HttpMethod.Post, "users", user.PolarAccessToken!,
            JsonContent.Create(new Dictionary<string, string> { ["member-id"] = user.PolarUserId }));
        if (!registerResponse.IsSuccessStatusCode && registerResponse.StatusCode != HttpStatusCode.Conflict)
            Console.WriteLine($"Polar-Registrierung fehlgeschlagen ({(int)registerResponse.StatusCode}): {await registerResponse.Content.ReadAsStringAsync()}");

        return Redirect($"{FrontendUrl}/?polar=connected");
    }

    // User bei Polar abmelden und Token löschen; lokal wird immer getrennt, auch wenn Polar den User nicht mehr kennt
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect(string email)
    {
        var user = await dbManager.FindUserByEmailAsync(email);
        if (user == null)
            return NotFound("User nicht gefunden");

        if (user.PolarAccessToken != null && user.PolarUserId != null)
        {
            var deleteResponse = await polarClient.SendAsync(HttpMethod.Delete, $"users/{user.PolarUserId}", user.PolarAccessToken);
            Console.WriteLine($"Polar-Abmeldung: {deleteResponse.StatusCode}");
        }

        user.PolarAccessToken = null;
        user.PolarUserId = null;
        await dbManager.SaveChangesAsync();

        return Ok(new { connected = false });
    }
}
