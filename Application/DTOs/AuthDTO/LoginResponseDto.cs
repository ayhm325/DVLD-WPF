namespace Application.DTOs.AuthDTO;

public sealed class LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }

    public int UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public int PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;
}