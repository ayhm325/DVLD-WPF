namespace Application.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }

    string Username { get; }

    string FullName { get; }

    string AccessToken { get; }

    bool IsLoggedIn { get; }

    void SetSession(
        int userId,
        string username,
        string fullName,
        string accessToken);

    void Clear();
}