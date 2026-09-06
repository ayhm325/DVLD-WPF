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
        ?? throw new ArgumentNullException(
            nameof(userRepository));

    private readonly IJwtTokenService _jwtTokenService =
        jwtTokenService
        ?? throw new ArgumentNullException(
            nameof(jwtTokenService));

    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto)
    {
        var validation =
            UserValidator.ValidateLogin(dto);

        if (validation.IsFailure)
        {
            return Result<LoginResponseDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var username =
            dto.UserName.Trim();

        var user =
            await _userRepository
                .GetUserByUsernameAsync(username);

        if (user is null ||
            !user.IsActive ||
            !BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.Password))
        {
            return Result<LoginResponseDto>
                .FromFailure(
                    "Invalid username or password.");
        }

        var userDto =
            UserMapper.ToDto(user);

        var token =
            _jwtTokenService
                .GenerateToken(userDto);

        return Result<LoginResponseDto>.Success(
            new LoginResponseDto
            {
                AccessToken = token.AccessToken,
                ExpiresAtUtc = token.ExpiresAtUtc,
                UserId = user.UserId,
                UserName = user.UserName,
                PersonId = user.PersonId,
                FullName = userDto.FullName,
                Role = user.Role
            });
    }
}