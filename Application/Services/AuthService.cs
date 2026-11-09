using System.Data;

using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Options;
using Application.Validators;

using Domain.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

/// <summary>
/// Owns the authentication lifecycle: login, refresh-token rotation with reuse
/// detection, logout, and session issuance after security-sensitive changes.
/// </summary>
public sealed class AuthService(
    IUserRepository userRepository,
    IUserSessionRepository sessionRepository,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IUnitOfWork unitOfWork,
    IJordanClock clock,
    IOptions<AuthSessionOptions> sessionOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly IUserRepository _userRepository =
        userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    private readonly IUserSessionRepository _sessionRepository =
        sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    private readonly IJwtTokenService _jwtTokenService =
        jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    private readonly IRefreshTokenService _refreshTokenService =
        refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IJordanClock _clock =
        clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly AuthSessionOptions _options =
        sessionOptions?.Value ?? throw new ArgumentNullException(nameof(sessionOptions));
    private readonly ILogger<AuthService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validation = UserValidator.ValidateLogin(dto);

        if (validation.IsFailure)
        {
            return Result<LoginResponseDto>.FromValidationFailure(validation.Error);
        }

        var username = dto.UserName.Trim();

        var user = await _userRepository.GetUserByUsernameAsync(
            username, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            // Deliberately identical for unknown user, inactive user, and wrong
            // password: no account enumeration, no credential material in the log.
            _logger.LogWarning(
                "Failed login attempt for user name {UserName}.",
                username);

            return Result<LoginResponseDto>.FromUnauthorized(
                "Invalid username or password.");
        }

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<Result<LoginResponseDto>> RefreshAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!_refreshTokenService.LooksLikeValidToken(refreshToken))
        {
            return Unauthorized();
        }

        var now = _clock.UtcNow;
        var tokenHash = _refreshTokenService.HashToken(refreshToken);

        var session = await _sessionRepository.GetByTokenHashAsync(
            tokenHash, cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetUserByUserIdAsync(
            session.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await _sessionRepository.RevokeAllForUserAsync(
                session.UserId,
                now,
                SessionRevocationReasons.AccountDisabled,
                cancellationToken);

            return Unauthorized();
        }

        if (session.RevokedAtUtc is not null)
        {
            return await HandleConsumedTokenAsync(
                session, now, cancellationToken);
        }

        if (session.ExpiresAtUtc <= now ||
            now - session.LastUsedAtUtc >
            TimeSpan.FromDays(Math.Max(1, _options.IdleTimeoutDays)))
        {
            await _sessionRepository.RevokeFamilyAsync(
                session.TokenFamilyId,
                now,
                SessionRevocationReasons.Expired,
                cancellationToken);

            return Unauthorized();
        }

        var newRawToken = _refreshTokenService.CreateToken();

        var newSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            RefreshTokenHash = _refreshTokenService.HashToken(newRawToken),
            TokenFamilyId = session.TokenFamilyId,
            CreatedAtUtc = now,
            // Absolute family expiry is never extended by rotation.
            ExpiresAtUtc = session.ExpiresAtUtc,
            LastUsedAtUtc = now
        };

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async transaction =>
                {
                    // Single conditional UPDATE: exactly one concurrent caller can
                    // consume this generation. The database row lock is the arbiter,
                    // so the guarantee holds across application instances.
                    var consumed = await _sessionRepository.TryConsumeAsync(
                        session.Id,
                        now,
                        newSession.Id,
                        cancellationToken);

                    if (!consumed)
                    {
                        return Result<LoginResponseDto>.FromConflict(
                            "A token refresh is already in progress. Retry the request.");
                    }

                    await _sessionRepository.AddAsync(newSession, cancellationToken);

                    if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0)
                    {
                        return Result<LoginResponseDto>.FromFailure(
                            "Failed to persist the refreshed authentication session.");
                    }

                    await transaction.CommitAsync(cancellationToken);

                    return Result<LoginResponseDto>.Success(
                        BuildResponse(user, newRawToken, newSession.ExpiresAtUtc));
                },
                IsolationLevel.ReadCommitted,
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Refresh-token rotation failed for user {UserId}.",
                session.UserId);

            return Result<LoginResponseDto>.FromFailure(
                "Failed to rotate the authentication session.");
        }
    }

    public async Task<Result> LogoutAsync(
        string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!_refreshTokenService.LooksLikeValidToken(refreshToken))
        {
            return Result.Success();
        }

        await _sessionRepository.RevokeByTokenHashAsync(
            _refreshTokenService.HashToken(refreshToken!),
            _clock.UtcNow,
            SessionRevocationReasons.Logout,
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> LogoutAllAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return Result.Unauthorized(
                "Authenticated user is required.");
        }

        await _sessionRepository.RevokeAllForUserAsync(
            userId,
            _clock.UtcNow,
            SessionRevocationReasons.LogoutAll,
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result<LoginResponseDto>> StartSessionAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetUserByUserIdAsync(
            userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<bool> IsPrincipalCurrentAsync(
        int userId, string securityStamp, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(securityStamp))
        {
            return false;
        }

        var state = await _userRepository.GetSecurityStateAsync(
            userId, cancellationToken);

        return state is not null &&
               state.IsActive &&
               string.Equals(
                   state.SecurityStamp,
                   securityStamp,
                   StringComparison.Ordinal);
    }

    private async Task<Result<LoginResponseDto>> HandleConsumedTokenAsync(
        UserSession session,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var revokedAt = session.RevokedAtUtc!.Value;

        var isBenignConcurrentRefresh =
            string.Equals(
                session.RevocationReason,
                SessionRevocationReasons.Rotated,
                StringComparison.Ordinal) &&
            now - revokedAt <=
            TimeSpan.FromSeconds(Math.Max(1, _options.RotationGraceSeconds)) &&
            await _sessionRepository.HasActiveSessionInFamilyAsync(
                session.TokenFamilyId, now, cancellationToken);

        if (isBenignConcurrentRefresh)
        {
            // A legitimate client raced itself (two tabs). No family revocation: the
            // winning rotation is still active, so the client can retry and receive
            // its already-issued cookie.
            return Result<LoginResponseDto>.FromConflict(
                "A token refresh is already in progress. Retry the request.");
        }

        // Confirmed reuse of a consumed generation: revoke the whole family.
        await _sessionRepository.RevokeFamilyAsync(
            session.TokenFamilyId,
            now,
            SessionRevocationReasons.ReuseDetected,
            cancellationToken);

        _logger.LogWarning(
            "Refresh-token reuse detected for user {UserId}. Token family {TokenFamilyId} was revoked.",
            session.UserId,
            session.TokenFamilyId);

        return Unauthorized();
    }

    private async Task<Result<LoginResponseDto>> CreateSessionAsync(
        User user, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var rawToken = _refreshTokenService.CreateToken();

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            RefreshTokenHash = _refreshTokenService.HashToken(rawToken),
            TokenFamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(
                Math.Max(1, _options.RefreshTokenLifetimeDays)),
            LastUsedAtUtc = now
        };

        await _sessionRepository.AddAsync(session, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0)
        {
            return Result<LoginResponseDto>.FromFailure(
                "Failed to create the authentication session.");
        }

        return Result<LoginResponseDto>.Success(
            BuildResponse(user, rawToken, session.ExpiresAtUtc));
    }

    private LoginResponseDto BuildResponse(
        User user,
        string refreshToken,
        DateTime refreshExpiresAtUtc)
    {
        var fullName = user.Person?.FullName ?? user.UserName;

        var token = _jwtTokenService.GenerateToken(
            new JwtTokenRequest
            {
                UserId = user.UserId,
                UserName = user.UserName,
                FullName = fullName,
                PersonId = user.PersonId,
                Role = user.Role,
                SecurityStamp = user.SecurityStamp
            });

        return new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            UserId = user.UserId,
            UserName = user.UserName,
            PersonId = user.PersonId,
            FullName = fullName,
            Role = user.Role,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc
        };
    }

    private static Result<LoginResponseDto> Unauthorized() =>
        Result<LoginResponseDto>.FromUnauthorized(
            "The authentication session is invalid or has expired.");
}
