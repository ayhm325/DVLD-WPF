using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;

using DVLD.Api.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [FromBody] ContractAuth.LoginRequest request)
    {
        var result = await authService.LoginAsync(
            new LoginRequestDto
            {
                UserName = request.UserName,
                Password = request.Password
            });

        if (result.IsFailure)
        {
            return result.ErrorType == ErrorType.Validation
                ? BadRequest(new { error = result.Error })
                : Unauthorized(new
                {
                    error = "Invalid username or password."
                });
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
    public async Task<IActionResult> Profile()
    {
        var result =
            await userService.GetCurrentProfileAsync(
                currentUserService.UserId);

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
        ContractAuth.ChangePasswordRequest request)
    {
        var result =
            await userService.ChangePasswordAsync(
                currentUserService.UserId,
                new ChangePasswordDto
                {
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword
                });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }
}