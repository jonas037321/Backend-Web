using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;

namespace Api.Controllers;

public record AuthRequest(string Email, string Password);

[ApiController]
[Route("api")]
public class AuthController(DbManager dbManager) : ControllerBase
{
    private static readonly PasswordHasher<User> PasswordHasher = new();

    [HttpPost("registration")]
    public async Task<IActionResult> Register(AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "E-Mail und Passwort dürfen nicht leer sein." });

        if (await dbManager.FindUserByEmailAsync(request.Email) != null)
            return Conflict(new { message = "Ein Benutzer mit dieser E-Mail-Adresse existiert bereits." });

        var user = new User { Email = request.Email.Trim().ToLowerInvariant() };
        user.Password = PasswordHasher.HashPassword(user, request.Password);
        await dbManager.AddUserAsync(user);

        return StatusCode(StatusCodes.Status201Created, new { message = "Registrierung erfolgreich." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "E-Mail und Passwort müssen angegeben werden." });

        var user = await dbManager.FindUserByEmailAsync(request.Email);

        // Gleiche Meldung für unbekannte E-Mail und falsches Passwort, damit niemand registrierte Adressen herausfinden kann
        if (user == null || PasswordHasher.VerifyHashedPassword(user, user.Password, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Ungültige E-Mail-Adresse oder Passwort." });

        return Ok(new
        {
            message = "Login erfolgreich.",
            user = new
            {
                id = user.Id,
                email = user.Email,
                isPolarConnected = !string.IsNullOrEmpty(user.PolarAccessToken)
            }
        });
    }
}
