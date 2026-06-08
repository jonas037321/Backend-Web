using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly DbManager _dbManager;
        private readonly PasswordHasher<User> _passwordHasher;

        public LoginController(DbManager dbManager)
        {
            _dbManager = dbManager;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            // 1. Validierung der Eingaben
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "E-Mail und Passwort müssen angegeben werden." });
            }

            try
            {
                // 2. Benutzer anhand der E-Mail in der Datenbank suchen
                var user = await _dbManager.FindUserByEmailAsync(request.Email, cancellationToken);

                // Sicherheitstipp: Wenn der User nicht existiert, geben wir trotzdem "Falsche E-Mail oder Passwort" zurück.
                // Das verhindert, dass Angreifer herausfinden können, welche E-Mail-Adressen registriert sind.
                if (user == null)
                {
                    return Unauthorized(new { message = "Ungültige E-Mail-Adresse oder Passwort." });
                }

                // 3. Passwort überprüfen
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    return Unauthorized(new { message = "Ungültige E-Mail-Adresse oder Passwort." });
                }

                // 4. Login erfolgreich!
                // Wenn du später JWT-Tokens nutzt, würdest du hier das Token generieren und mitschicken.
                return Ok(new
                {
                    message = "Login erfolgreich.",
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        // Wir schicken dem Frontend ein Flag mit, ob der User schon mit Polar verknüpft ist
                        isPolarConnected = !string.IsNullOrEmpty(user.PolarAccessToken)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ein interner Fehler ist aufgetreten.", detail = ex.Message });
            }
        }
    }

    // Hilfsklasse (DTO) für die Login-Daten
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}