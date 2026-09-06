using Domain.Enums;

namespace Application.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }

    string Username { get; }

    string FullName { get; }

    UserRole Role { get; }

    string AccessToken { get; }

    bool IsLoggedIn { get; }

    void SetSession(
        int userId,
        string username,
        string fullName,
        UserRole role,
        string accessToken);

    void Clear();
}