using Application.Interfaces;

namespace Presentation.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    public int UserId { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string AccessToken { get; private set; } = string.Empty;

    public bool IsLoggedIn =>
        UserId > 0 &&
        !string.IsNullOrWhiteSpace(AccessToken);

    public void SetSession(
        int userId,
        string username,
        string fullName,
        string accessToken)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException(
                "Access token is required.",
                nameof(accessToken));

        UserId = userId;
        Username = username?.Trim() ?? string.Empty;
        FullName = fullName?.Trim() ?? string.Empty;
        AccessToken = accessToken;
    }

    public void Clear()
    {
        UserId = 0;
        Username = string.Empty;
        FullName = string.Empty;
        AccessToken = string.Empty;
    }
}