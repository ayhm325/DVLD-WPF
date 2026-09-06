using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [FromBody] LoginRequestDto dto)
    {
        var result =
            await authService.LoginAsync(dto);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorType switch
        {
            ErrorType.Validation =>
                BadRequest(new { error = result.Error }),

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

        return Ok(new
        {
            userId = currentUserService.UserId,
            username = currentUserService.Username,
            fullName = currentUserService.FullName,
            role = currentUserService.Role
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto)
    {
        if (!currentUserService.IsLoggedIn)
            return Unauthorized();

        var result =
            await userService.ChangePasswordAsync(
                currentUserService.UserId,
                dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static IActionResult HandleFailure(
        Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(
                    new { error = result.Error }),

            ErrorType.NotFound =>
                new NotFoundObjectResult(
                    new { error = result.Error }),

            ErrorType.Conflict =>
                new ConflictObjectResult(
                    new { error = result.Error }),

            ErrorType.Forbidden =>
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}