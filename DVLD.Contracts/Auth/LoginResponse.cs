namespace DVLD.Contracts.Auth;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }

    public int UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public int PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
