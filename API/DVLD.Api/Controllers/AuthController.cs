using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractAuth = DVLD.Contracts.Auth;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService,
    IUserService userService,
    ICurrentUserService currentUserService)
    : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] ContractAuth.LoginRequest request)
    {
        var dto = new LoginRequestDto
        {
            UserName = request.UserName,
            Password = request.Password
        };

        var result =
            await authService.LoginAsync(dto);

        if (result.IsSuccess)
        {
            var response = result.Value!;

            return Ok(
                new ContractAuth.LoginResponse
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

        return result.ErrorType switch
        {
            ErrorType.Validation =>
                BadRequest(new
                {
                    error = result.Error
                }),

            _ =>
                Unauthorized(new
                {
                    error = "Invalid username or password."
                })
        };
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!currentUserService.IsLoggedIn)
            return Unauthorized();

        return Ok(
            new
            {
                userId = currentUserService.UserId,
                username = currentUserService.Username,
                fullName = currentUserService.FullName,
                role = currentUserService.Role.ToString()
            });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ContractAuth.ChangePasswordRequest request)
    {
        if (!currentUserService.IsLoggedIn)
            return Unauthorized();

        var dto = new ChangePasswordDto
        {
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        var result =
            await userService.ChangePasswordAsync(
                currentUserService.UserId,
                dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static IActionResult HandleFailure(Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(
                    new
                    {
                        error = result.Error
                    }),

            ErrorType.NotFound =>
                new NotFoundObjectResult(
                    new
                    {
                        error = result.Error
                    }),

            ErrorType.Conflict =>
                new ConflictObjectResult(
                    new
                    {
                        error = result.Error
                    }),

            ErrorType.Forbidden =>
                new ObjectResult(
                    new
                    {
                        error = result.Error
                    })
                {
                    StatusCode =
                        StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new
                    {
                        error = result.Error
                    })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}