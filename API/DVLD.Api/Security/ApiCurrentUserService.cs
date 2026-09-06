using Application.Interfaces;
using System.Security.Claims;

namespace DVLD.Api.Security;

public sealed class ApiCurrentUserService(
    IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor
        ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public int UserId =>
        int.TryParse(
            GetClaim(ClaimTypes.NameIdentifier),
            out var userId)
            ? userId
            : 0;

    public string Username =>
        GetClaim(ClaimTypes.Name);

    public string FullName =>
        GetClaim("FullName");

    public string AccessToken =>
        string.Empty;

    public bool IsLoggedIn =>
        UserId > 0;

    public void SetSession(
        int userId,
        string username,
        string fullName,
        string accessToken)
    {
        throw new NotSupportedException(
            "The API current user is provided by the authenticated request.");
    }

    public void Clear()
    {
        throw new NotSupportedException(
            "The API current user is provided by the authenticated request.");
    }

    private string GetClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?
            .User
            .FindFirst(claimType)?
            .Value
            ?? string.Empty;
    }
}