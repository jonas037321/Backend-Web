namespace Api.Contracts.Auth;

public sealed class LoginResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
