using System.Security.Claims;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DVLD.Api.Security;

public sealed class ApiCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiCurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var userId)
                ? userId
                : 0;
        }
        set => throw new NotSupportedException(
            "UserId is read-only in the API.");
    }

    public string Username
    {
        get =>
            _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;

        set => throw new NotSupportedException(
            "Username is read-only in the API.");
    }

    public string FullName
    {
        get =>
            _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue("FullName")
            ?? string.Empty;

        set => throw new NotSupportedException(
            "FullName is read-only in the API.");
    }

    public bool IsLoggedIn =>
        _httpContextAccessor.HttpContext?
            .User
            .Identity?
            .IsAuthenticated
        == true;

    public void Clear()
    {
        // Authentication state is managed by the JWT/request pipeline.
    }
}