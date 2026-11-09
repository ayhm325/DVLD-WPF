using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;

using DVLD.Api.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

using ContractAuth = DVLD.Contracts.Auth;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService,
    IUserService userService,
    ICurrentUserService currentUserService) : ControllerBase
{
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
            // Validation -> 400 ValidationProblemDetails; invalid credentials ->
            // 401 ProblemDetails. Mapping is centralized in ResultHttpMapper.
            return result.ToActionResult(this);
        }

        var response = result.Value!;

        return Ok(new ContractAuth.LoginResponse
        {
            AccessToken = response.AccessToken,
            ExpiresAtUtc = response.ExpiresAtUtc,
            UserId = response.UserId,
            UserName = response.UserName,
            PersonId = response.PersonId,
            FullName = response.FullName,
            Role = response.Role.ToString()
        });
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

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private IActionResult UnexpectedResult(string diagnosticMessage)
    {
        HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<AuthController>()
            .LogError(
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
