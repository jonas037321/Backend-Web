using Api.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using ORM;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly DbManager _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(DbManager db)
    {
        _db = db;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "firstName, lastName, email and password are required." });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (!IsValidPassword(request.Password, out var passwordError))
        {
            return BadRequest(new { message = passwordError });
        }

        var emailExists = await _db.Users.AnyAsync(user => user.Email.ToLower() == email, cancellationToken);
        if (emailExists)
        {
            return Conflict(new { message = "A user with this e-mail already exists." });
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
        };

        user.Password = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };

        return Created("/api/auth/register", response);
    }

    private static bool IsValidPassword(string password, out string error)
    {
        if (password.Length < 8)
        {
            error = "Password must be at least 8 characters long.";
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            error = "Password must contain at least one uppercase letter.";
            return false;
        }

        if (!password.Any(char.IsLower))
        {
            error = "Password must contain at least one lowercase letter.";
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            error = "Password must contain at least one number.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
