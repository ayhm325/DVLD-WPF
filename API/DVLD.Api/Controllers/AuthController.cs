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
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto dto)
    {
        var result = await authService.LoginAsync(dto);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(new { error = result.Error }),
            ErrorType.Forbidden => Forbid(),
            _ => Unauthorized(new { error = result.Error })
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
            fullName = currentUserService.FullName
        });
    }
}