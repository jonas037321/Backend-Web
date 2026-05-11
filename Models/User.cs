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
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Birthdate { get; set; }
    public Gender Gender { get; set; }
    public string Password { get; set; } = string.Empty;
}
