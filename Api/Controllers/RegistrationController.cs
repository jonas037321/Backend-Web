using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Models;
using ORM;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly DbManager _dbManager;
        private readonly PasswordHasher<User> _passwordHasher;

        // Der DbManager wird injiziert, den PasswordHasher erstellen wir direkt
        public RegistrationController(DbManager dbManager)
        {
            _dbManager = dbManager;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            // 1. Validierung der Eingaben
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "E-Mail und Passwort dürfen nicht leer sein." });
            }

            try
            {
                // 2. Prüfen, ob der Benutzer bereits existiert
                var existingUser = await _dbManager.FindUserByEmailAsync(request.Email, cancellationToken);
                if (existingUser != null)
                {
                    return Conflict(new { message = "Ein Benutzer mit dieser E-Mail-Adresse existiert bereits." });
                }

                // 3. Neues User-Objekt erstellen (noch ohne Passwort)
                var newUser = new User
                {
                    Email = request.Email.Trim().ToLowerInvariant()
                };

                // 4. Passwort mit dem PasswordHasher verschlüsseln
                // Der Hasher benötigt die User-Instanz als Kontext (u.a. für potenzielle Security-Stamps)
                newUser.Password = _passwordHasher.HashPassword(newUser, request.Password);

                // 5. In der Datenbank speichern
                await _dbManager.AddUserAsync(newUser, cancellationToken);

                return StatusCode(201, new { message = "Registrierung erfolgreich." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ein interner Fehler ist aufgetreten.", detail = ex.Message });
            }
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}