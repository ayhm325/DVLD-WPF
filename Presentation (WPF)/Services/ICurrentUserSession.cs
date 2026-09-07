namespace Presentation.Services;

public interface ICurrentUserSession
{
    int UserId { get; }

    string Username { get; }

    string FullName { get; }

    string Role { get; }

    string AccessToken { get; }

    bool IsLoggedIn { get; }

    void SetSession(
        int userId,
        string username,
        string fullName,
        string role,
        string accessToken);

    void Clear();
}