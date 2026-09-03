using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto dto)
    {
        var result =
            await _authService.LoginAsync(dto);

        if (result.IsFailure)
        {
            return Unauthorized(new
            {
                message = result.Error
            });
        }

        return Ok(result.Value);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        if (!_currentUserService.IsLoggedIn)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            userId = _currentUserService.UserId,
            username = _currentUserService.Username,
            fullName = _currentUserService.FullName
        });
    }
}