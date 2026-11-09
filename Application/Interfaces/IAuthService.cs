using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the refresh token and issues a new access token. The supplied token is
    /// consumed atomically; exactly one concurrent caller can succeed.
    /// </summary>
    Task<Result<LoginResponseDto>> RefreshAsync(
        string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes the session that owns the token. Idempotent and never throws for unknown tokens.</summary>
    Task<Result> LogoutAsync(
        string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active session belonging to the user.</summary>
    Task<Result> LogoutAllAsync(
        int userId, CancellationToken cancellationToken = default);

    /// <summary>Creates a brand-new session after an authenticated security change (password change).</summary>
    Task<Result<LoginResponseDto>> StartSessionAsync(
        int userId, CancellationToken cancellationToken = default);

    /// <summary>Validates the token's security stamp against the current persisted state.</summary>
    Task<bool> IsPrincipalCurrentAsync(
        int userId, string securityStamp, CancellationToken cancellationToken = default);
}
