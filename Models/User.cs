namespace Models;

public enum Gender
{
    Male,
    Female,
    Other
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PolarAccessToken { get; set; }
    public string? PolarUserId { get; set; }
}
