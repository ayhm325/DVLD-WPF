using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService) : IAuthService
{
    private readonly IUserRepository _userRepository =
        userRepository
        ?? throw new ArgumentNullException(nameof(userRepository));

    private readonly IJwtTokenService _jwtTokenService =
        jwtTokenService
        ?? throw new ArgumentNullException(nameof(jwtTokenService));

    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto)
    {
        // =========================================================
        // 1. VALIDATION
        // =========================================================

        var validationResult =
            UserValidator.ValidateLogin(dto);

        if (validationResult.IsFailure)
        {
            return Result<LoginResponseDto>.FromValidationFailure(
                validationResult.Error);
        }

        // =========================================================
        // 2. NORMALIZE USERNAME
        // =========================================================

        var username = dto.UserName.Trim();

        // =========================================================
        // 3. FIND USER
        // =========================================================

        var user =
            await _userRepository.GetUserByUsernameAsync(username);

        // =========================================================
        // 4. AUTHENTICATION
        // =========================================================

        if (user is null ||
            !user.IsActive ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            return Result<LoginResponseDto>.FromForbidden(
                "Invalid username or password.");
        }

        // =========================================================
        // 5. MAP USER
        // =========================================================

        var userDto = UserMapper.ToDto(user);

        // =========================================================
        // 6. GENERATE JWT
        // =========================================================

        var tokenResult =
            _jwtTokenService.GenerateToken(userDto);

        // =========================================================
        // 7. BUILD RESPONSE
        // =========================================================

        var response = new LoginResponseDto
        {
            AccessToken = tokenResult.AccessToken,
            ExpiresAtUtc = tokenResult.ExpiresAtUtc,
            UserId = user.UserId,
            UserName = user.UserName,
            PersonId = user.PersonId,
            FullName = userDto.FullName
        };

        // =========================================================
        // 8. SUCCESS
        // =========================================================

        return Result<LoginResponseDto>.Success(response);
    }
}