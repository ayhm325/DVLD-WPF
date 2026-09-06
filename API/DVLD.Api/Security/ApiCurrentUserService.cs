using System.Security.Claims;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace DVLD.Api.Security;

public sealed class ApiCurrentUserService(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor
        ?? throw new ArgumentNullException(
            nameof(httpContextAccessor));

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

    public UserRole Role =>
        Enum.TryParse<UserRole>(
            GetClaim(ClaimTypes.Role),
            out var role)
            ? role
            : default;

    public string AccessToken =>
        string.Empty;

    public bool IsLoggedIn =>
        UserId > 0;

    public void SetSession(
        int userId,
        string username,
        string fullName,
        UserRole role,
        string accessToken)
    {
        throw new NotSupportedException(
            "API current user state is provided by the authenticated HTTP request.");
    }

    public void Clear()
    {
        throw new NotSupportedException(
            "API current user state is provided by the authenticated HTTP request.");
    }

    private string GetClaim(string claimType)
    {
        return _httpContextAccessor
            .HttpContext?
            .User
            .FindFirst(claimType)?
            .Value
            ?? string.Empty;
    }
}