using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository
            ?? throw new ArgumentNullException(nameof(userRepository));

        _jwtTokenService = jwtTokenService
            ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

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
            return Result<LoginResponseDto>.FromFailure(
                validationResult.Error);
        }


        // =========================================================
        // 2. NORMALIZE USERNAME
        // =========================================================

        var username =
            dto.UserName.Trim();


        // =========================================================
        // 3. FIND USER
        // =========================================================

        var user =
            await _userRepository.GetUserByUsernameAsync(
                username);

        if (user is null)
        {
            return Result<LoginResponseDto>.FromFailure(
                "Invalid username or password.");
        }


        // =========================================================
        // 4. CHECK USER STATUS
        // =========================================================

        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.FromFailure(
                "Invalid username or password.");
        }


        // =========================================================
        // 5. VERIFY PASSWORD
        // =========================================================

        var passwordValid =
            BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.Password);

        if (!passwordValid)
        {
            return Result<LoginResponseDto>.FromFailure(
                "Invalid username or password.");
        }


        // =========================================================
        // 6. MAP USER
        // =========================================================

        var userDto =
            UserMapper.ToDto(user);


        // =========================================================
        // 7. GENERATE JWT
        // =========================================================

        var tokenResult =
            _jwtTokenService.GenerateToken(
                userDto);


        // =========================================================
        // 8. BUILD RESPONSE
        // =========================================================

        var response =
            new LoginResponseDto
            {
                AccessToken =
                    tokenResult.AccessToken,

                ExpiresAtUtc =
                    tokenResult.ExpiresAtUtc,

                UserId =
                    user.UserId,

                UserName =
                    user.UserName,

                PersonId =
                    user.PersonId,

                FullName =
                    userDto.FullName
            };


        // =========================================================
        // 9. SUCCESS
        // =========================================================

        return Result<LoginResponseDto>.Success(
            response);
    }
}