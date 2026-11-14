using System.Text;

using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Options;

using DVLD.Api.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ContractAuth = DVLD.Contracts.Auth;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService,
    IUserService userService,
    ICurrentUserService currentUserService,
    IOptions<AuthSessionOptions> sessionOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    private const string CsrfHeaderName = "X-CSRF-Protection";
    private const string CsrfHeaderValue = "1";

    private readonly AuthSessionOptions _sessionOptions =
        sessionOptions?.Value ?? throw new ArgumentNullException(nameof(sessionOptions));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginRateLimit")]
    public async Task<IActionResult> Login(
        [FromBody] ContractAuth.LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(
            new LoginRequestDto
            {
                UserName = request.UserName,
                Password = request.Password
            }, cancellationToken);

        if (result is null)
        {
            return UnexpectedResult(
                "The authentication service returned a null result.");
        }

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var response = result.Value!;
        SetRefreshCookieIfPresent(response);

        return Ok(MapLoginResponse(response));
    }

    /// <summary>
    /// Rotates the refresh token. Anonymous by design: the previous access token is
    /// expected to be expired, and the HttpOnly cookie is the credential.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("RefreshRateLimit")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken = default)
    {
        if (!HasCsrfHeader())
        {
            return CsrfFailure();
        }

        var refreshToken = ReadRefreshCookie();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            ClearRefreshCookie();
            return UnauthorizedProblem();
        }

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);

        if (result.IsFailure)
        {
            // Only a definitive authentication failure clears the cookie. A concurrent
            // refresh (409) must leave the winner's cookie intact.
            if (result.ErrorType == ErrorType.Unauthorized)
            {
                ClearRefreshCookie();
            }

            return result.ToActionResult(this);
        }

        var response = result.Value!;
        SetRefreshCookieIfPresent(response);

        return Ok(MapLoginResponse(response));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        if (!HasCsrfHeader())
        {
            return CsrfFailure();
        }

        await authService.LogoutAsync(ReadRefreshCookie(), cancellationToken);

        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken = default)
    {
        if (!HasCsrfHeader())
        {
            return CsrfFailure();
        }

        var result = await authService.LogoutAllAsync(
            currentUserService.UserId, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    [HttpGet("me")]
    public IActionResult Me()
        => Ok(new
        {
            userId = currentUserService.UserId,
            username = currentUserService.Username,
            fullName = currentUserService.FullName,
            role = currentUserService.Role.ToString()
        });

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken = default)
    {
        var result =
            await userService.GetCurrentProfileAsync(
                currentUserService.UserId, cancellationToken);

        if (result.IsFailure)
            return result.ToActionResult(this);

        var profile = result.Value!;

        return Ok(
            new DVLD.Contracts.User.UserProfileResponse(
                profile.UserId,
                profile.PersonId,
                profile.UserName,
                profile.IsActive,
                profile.FullName,
                profile.NationalNo,
                profile.FirstName,
                profile.SecondName,
                profile.ThirdName,
                profile.LastName,
                profile.DateOfBirth,
                profile.Gender,
                profile.Address,
                profile.Phone,
                profile.Email,
                profile.NationalityCountryID,
                profile.CountryName,
                profile.ImagePath));
    }

    [HttpPost("change-password")]
    [EnableRateLimiting("ChangePasswordRateLimit")]
    public async Task<IActionResult> ChangePassword(
        [FromBody]
        ContractAuth.ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var result =
            await userService.ChangePasswordAsync(
                currentUserService.UserId,
                new ChangePasswordDto
                {
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword
                }, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        // The password change was committed and revoked every session, including this one.
        // Attempt to issue a fresh session for the current device so the user is not logged out.
        // Session establishment failure is handled separately from the already-committed password change.
        Result<LoginResponseDto> session;
        try
        {
            session = await authService.StartSessionAsync(
                currentUserService.UserId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Password changed successfully for user {UserId}, but fresh session issuance threw an exception.",
                currentUserService.UserId);

            ClearRefreshCookie();

            return ApiProblemDetails.ToActionResult(
                HttpContext,
                ApiProblemDetails.Create(
                    HttpContext,
                    StatusCodes.Status500InternalServerError,
                    "Session establishment failed",
                    "Your password was changed successfully and all existing sessions were revoked, but establishing a new session failed. Please log in with your new password.",
                    ProblemTypes.Internal));
        }

        if (session.IsFailure)
        {
            logger.LogError(
                "Password changed successfully for user {UserId}, but fresh session issuance failed: {Error}",
                currentUserService.UserId,
                session.Error);

            ClearRefreshCookie();

            return ApiProblemDetails.ToActionResult(
                HttpContext,
                ApiProblemDetails.Create(
                    HttpContext,
                    StatusCodes.Status500InternalServerError,
                    "Session establishment failed",
                    "Your password was changed successfully and all existing sessions were revoked, but establishing a new session failed. Please log in with your new password.",
                    ProblemTypes.Internal));
        }

        var response = session.Value!;
        SetRefreshCookieIfPresent(response);

        return Ok(MapLoginResponse(response));
    }

    private static ContractAuth.LoginResponse MapLoginResponse(LoginResponseDto response) =>
        new()
        {
            AccessToken = response.AccessToken,
            ExpiresAtUtc = response.ExpiresAtUtc,
            UserId = response.UserId,
            UserName = response.UserName,
            PersonId = response.PersonId,
            FullName = response.FullName,
            Role = response.Role.ToString()
        };

    private string? ReadRefreshCookie() =>
        Request.Cookies.TryGetValue(_sessionOptions.RefreshCookieName, out var value)
            ? value
            : null;

    private bool HasCsrfHeader() =>
        string.Equals(
            Request.Headers[CsrfHeaderName].ToString(),
            CsrfHeaderValue,
            StringComparison.Ordinal);

    private IActionResult CsrfFailure() =>
        Result
            .ValidationFailure(
                $"The required {CsrfHeaderName} header is missing.")
            .ToActionResult(this);

    private void SetRefreshCookieIfPresent(LoginResponseDto response)
    {
        if (string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            return;
        }

        SetRefreshCookie(response.RefreshToken, response.RefreshTokenExpiresAtUtc);
    }

    private void SetRefreshCookie(string token, DateTime expiresAtUtc)
    {
        var options = CreateCookieOptions();
        options.Expires = new DateTimeOffset(
            DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc));

        Response.Cookies.Append(_sessionOptions.RefreshCookieName, token, options);
    }

    private void ClearRefreshCookie()
    {
        var options = CreateCookieOptions();
        Response.Cookies.Delete(_sessionOptions.RefreshCookieName, options);
    }

    private CookieOptions CreateCookieOptions() =>
        new()
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = _sessionOptions.RefreshCookiePath,
            IsEssential = true
        };

    private IActionResult UnauthorizedProblem() =>
        Result<LoginResponseDto>
            .FromUnauthorized(
                "The authentication session is invalid or has expired.")
            .ToActionResult(this);

    private IActionResult UnexpectedResult(string diagnosticMessage)
    {
        logger.LogError(
            "Authentication endpoint failed unexpectedly: {DiagnosticMessage}",
            diagnosticMessage);

        return ApiProblemDetails.ToActionResult(
            HttpContext,
            ApiProblemDetails.Create(
                HttpContext,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "The server could not complete the request.",
                ProblemTypes.Internal));
    }
}
