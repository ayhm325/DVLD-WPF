using System.Data;

using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Options;
using Application.Services;
using Application.UnitTests.Common;

using Domain.Entities;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace Application.UnitTests.Services;

public sealed class AuthServiceTests
{
    private static readonly DateTime NowUtc =
        new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserSessionRepository> _sessionRepository = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly IRefreshTokenService _refreshTokenService = new RefreshTokenService();
    private readonly FakeJordanClock _clock = FakeJordanClock.FromUtc(NowUtc);
    private readonly AuthSessionOptions _options = new();

    public AuthServiceTests()
    {
        _unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _transaction
            .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(unit => unit.ExecuteInTransactionAsync<Result<LoginResponseDto>>(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<LoginResponseDto>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (Func<IUnitOfWorkTransaction, Task<Result<LoginResponseDto>>> operation,
                    IsolationLevel _,
                    CancellationToken cancellationToken) =>
                    operation(_transaction.Object));

        _jwtTokenService
            .Setup(service => service.GenerateToken(It.IsAny<JwtTokenRequest>()))
            .Returns(new JwtTokenResult
            {
                AccessToken = "access-token",
                ExpiresAtUtc = NowUtc.AddMinutes(15)
            });
    }

    [Fact]
    public void Constructor_WhenUserRepositoryIsNull_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new AuthService(
                null!,
                _sessionRepository.Object,
                _jwtTokenService.Object,
                _refreshTokenService,
                _unitOfWork.Object,
                _clock,
                Microsoft.Extensions.Options.Options.Create(_options),
                NullLogger<AuthService>.Instance));

        Assert.Equal("userRepository", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Login_WhenUsernameIsMissing_ReturnsValidation(string? userName)
    {
        var result = await CreateService().LoginAsync(
            new LoginRequestDto { UserName = userName!, Password = "Password123!" });

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(
            repository => repository.GetUserByUsernameAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsGenericUnauthorized()
    {
        _userRepository
            .Setup(repository => repository.GetUserByUsernameAsync(
                "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateService().LoginAsync(Request("  admin  "));

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        Assert.Equal("Invalid username or password.", result.Error);
        _sessionRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<UserSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_WhenUserIsInactive_ReturnsGenericUnauthorized()
    {
        _userRepository
            .Setup(repository => repository.GetUserByUsernameAsync(
                "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(active: false));

        var result = await CreateService().LoginAsync(Request());

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ReturnsGenericUnauthorized()
    {
        _userRepository
            .Setup(repository => repository.GetUserByUsernameAsync(
                "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        var result = await CreateService().LoginAsync(
            Request(password: "WrongPassword123!"));

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _jwtTokenService.Verify(
            service => service.GenerateToken(It.IsAny<JwtTokenRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_PersistsOnlyTheRefreshTokenHash()
    {
        var user = CreateUser(id: 5);
        UserSession? persisted = null;

        _userRepository
            .Setup(repository => repository.GetUserByUsernameAsync(
                "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _sessionRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
            .Callback<UserSession, CancellationToken>((session, _) => persisted = session)
            .Returns(Task.CompletedTask);

        var result = await CreateService().LoginAsync(Request());

        Assert.True(result.IsSuccess);
        var response = result.Value!;

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal(5, response.UserId);
        Assert.NotNull(persisted);
        Assert.Equal(5, persisted!.UserId);
        Assert.True(_refreshTokenService.LooksLikeValidToken(response.RefreshToken));
        Assert.Equal(
            _refreshTokenService.HashToken(response.RefreshToken),
            persisted.RefreshTokenHash);
        Assert.NotEqual(response.RefreshToken, persisted.RefreshTokenHash);
        Assert.Equal(
            NowUtc.AddDays(_options.RefreshTokenLifetimeDays),
            persisted.ExpiresAtUtc);
        Assert.NotEqual(Guid.Empty, persisted.TokenFamilyId);

        _unitOfWork.Verify(
            unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_WhenSessionCannotBePersisted_ReturnsFailure()
    {
        _userRepository
            .Setup(repository => repository.GetUserByUsernameAsync(
                "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        _unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await CreateService().LoginAsync(Request());

        Assert.Equal(ErrorType.Failure, result.ErrorType);
    }

    [Fact]
    public async Task Refresh_WhenTokenFormatIsInvalid_ReturnsUnauthorizedWithoutLookup()
    {
        var result = await CreateService().RefreshAsync("not-a-token");

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _sessionRepository.Verify(
            repository => repository.GetByTokenHashAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenSessionIsUnknown_ReturnsUnauthorized()
    {
        var token = _refreshTokenService.CreateToken();

        _sessionRepository
            .Setup(repository => repository.GetByTokenHashAsync(
                _refreshTokenService.HashToken(token), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession?)null);

        var result = await CreateService().RefreshAsync(token);

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Refresh_WhenSessionIsActive_RotatesAtomically()
    {
        var user = CreateUser();
        var token = _refreshTokenService.CreateToken();
        var session = CreateSession(user, token);
        UserSession? added = null;

        SetupSessionLookup(token, session);
        SetupUser(user);
        _sessionRepository
            .Setup(repository => repository.TryConsumeAsync(
                session.Id, NowUtc, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _sessionRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
            .Callback<UserSession, CancellationToken>((newSession, _) => added = newSession)
            .Returns(Task.CompletedTask);

        var result = await CreateService().RefreshAsync(token);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(session.TokenFamilyId, added!.TokenFamilyId);
        Assert.Equal(session.ExpiresAtUtc, added.ExpiresAtUtc);
        Assert.Equal(NowUtc, added.LastUsedAtUtc);
        Assert.Equal(
            _refreshTokenService.HashToken(result.Value!.RefreshToken),
            added.RefreshTokenHash);
        Assert.NotEqual(token, result.Value.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WhenTokenWasConsumedOutsideGrace_RevokesFamily()
    {
        var user = CreateUser();
        var token = _refreshTokenService.CreateToken();
        var session = CreateSession(user, token);
        session.RevokedAtUtc = NowUtc.AddMinutes(-5);
        session.RevocationReason = SessionRevocationReasons.Rotated;

        SetupSessionLookup(token, session);
        SetupUser(user);

        var result = await CreateService().RefreshAsync(token);

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _sessionRepository.Verify(
            repository => repository.RevokeFamilyAsync(
                session.TokenFamilyId,
                NowUtc,
                SessionRevocationReasons.ReuseDetected,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenConsumedWithinGraceAndFamilyIsActive_ReturnsConflictWithoutRevocation()
    {
        var user = CreateUser();
        var token = _refreshTokenService.CreateToken();
        var session = CreateSession(user, token);
        session.RevokedAtUtc = NowUtc.AddSeconds(-5);
        session.RevocationReason = SessionRevocationReasons.Rotated;

        SetupSessionLookup(token, session);
        SetupUser(user);
        _sessionRepository
            .Setup(repository => repository.HasActiveSessionInFamilyAsync(
                session.TokenFamilyId, NowUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().RefreshAsync(token);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _sessionRepository.Verify(
            repository => repository.RevokeFamilyAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenSessionIsExpired_RevokesFamily()
    {
        var user = CreateUser();
        var token = _refreshTokenService.CreateToken();
        var session = CreateSession(user, token);
        session.ExpiresAtUtc = NowUtc.AddSeconds(-1);

        SetupSessionLookup(token, session);
        SetupUser(user);

        var result = await CreateService().RefreshAsync(token);

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _sessionRepository.Verify(
            repository => repository.RevokeFamilyAsync(
                session.TokenFamilyId,
                NowUtc,
                SessionRevocationReasons.Expired,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenSessionExceededIdleTimeout_RevokesFamily()
    {
        var user = CreateUser();
        var token = _refreshTokenService.CreateToken();
        var session = CreateSession(user, token);
        session.LastUsedAtUtc = NowUtc.AddDays(-(_options.IdleTimeoutDays + 1));

        SetupSessionLookup(token, session);
        SetupUser(user);

        var result = await CreateService().RefreshAsync(token);

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _sessionRepository.Verify(
            repository => repository.RevokeFamilyAsync(
                session.TokenFamilyId,
                NowUtc,
                SessionRevocationReasons.Expired,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenUserIsInactive_RevokesAllSessions()
    {
        var user = CreateUser(active: false);
        var token = _refreshTokenService.CreateToken();
        var session = CreateSession(user, token);

        SetupSessionLookup(token, session);
        SetupUser(user);

        var result = await CreateService().RefreshAsync(token);

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _sessionRepository.Verify(
            repository => repository.RevokeAllForUserAsync(
                user.UserId,
                NowUtc,
                SessionRevocationReasons.AccountDisabled,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenAnotherCallerWinsTheRace_ReturnsConflictWithoutAdding()
    {
        var user = CreateUser();
        var token = _refreshTokenService.CreateToken();
        var session = CreateSession(user, token);

        SetupSessionLookup(token, session);
        SetupUser(user);
        _sessionRepository
            .Setup(repository => repository.TryConsumeAsync(
                session.Id, NowUtc, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().RefreshAsync(token);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _sessionRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<UserSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenTwoCallersRace_ExactlyOneRotationWins()
    {
        var user = CreateUser();
        var repository = new InMemorySessionRepository();

        _userRepository
            .Setup(repo => repo.GetUserByUsernameAsync(
                "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetupUser(user);

        var login = await CreateService(repository).LoginAsync(Request());
        Assert.True(login.IsSuccess);

        var token = login.Value!.RefreshToken;

        // Two independent service instances share one atomic store, mirroring two
        // application instances racing on the same database row.
        var results = await Task.WhenAll(
            CreateService(repository).RefreshAsync(token),
            CreateService(repository).RefreshAsync(token));

        Assert.Equal(1, results.Count(result => result.IsSuccess));
        Assert.Equal(1, results.Count(result => result.ErrorType == ErrorType.Conflict));
        Assert.Single(
            repository.Sessions.Where(session => session.RevokedAtUtc is null));
    }

    [Fact]
    public async Task Logout_WhenTokenIsInvalid_IsIdempotentAndSkipsPersistence()
    {
        var result = await CreateService().LogoutAsync("short");

        Assert.True(result.IsSuccess);
        _sessionRepository.Verify(
            repository => repository.RevokeByTokenHashAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Logout_WhenTokenIsValid_RevokesByHash()
    {
        var token = _refreshTokenService.CreateToken();

        var result = await CreateService().LogoutAsync(token);

        Assert.True(result.IsSuccess);
        _sessionRepository.Verify(
            repository => repository.RevokeByTokenHashAsync(
                _refreshTokenService.HashToken(token),
                NowUtc,
                SessionRevocationReasons.Logout,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAll_RevokesEverySessionForTheUser()
    {
        var result = await CreateService().LogoutAllAsync(7);

        Assert.True(result.IsSuccess);
        _sessionRepository.Verify(
            repository => repository.RevokeAllForUserAsync(
                7,
                NowUtc,
                SessionRevocationReasons.LogoutAll,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IsPrincipalCurrent_WhenStampMatchesAndAccountIsActive_ReturnsTrue()
    {
        _userRepository
            .Setup(repository => repository.GetSecurityStateAsync(
                5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSecurityState
            {
                IsActive = true,
                SecurityStamp = "stamp-1"
            });

        Assert.True(
            await CreateService().IsPrincipalCurrentAsync(5, "stamp-1"));
    }

    [Fact]
    public async Task IsPrincipalCurrent_WhenStampDiffers_ReturnsFalse()
    {
        _userRepository
            .Setup(repository => repository.GetSecurityStateAsync(
                5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSecurityState
            {
                IsActive = true,
                SecurityStamp = "stamp-2"
            });

        Assert.False(
            await CreateService().IsPrincipalCurrentAsync(5, "stamp-1"));
    }

    private AuthService CreateService() =>
        new(
            _userRepository.Object,
            _sessionRepository.Object,
            _jwtTokenService.Object,
            _refreshTokenService,
            _unitOfWork.Object,
            _clock,
            Microsoft.Extensions.Options.Options.Create(_options),
            NullLogger<AuthService>.Instance);

    private AuthService CreateService(IUserSessionRepository sessionRepository) =>
        new(
            _userRepository.Object,
            sessionRepository,
            _jwtTokenService.Object,
            _refreshTokenService,
            _unitOfWork.Object,
            _clock,
            Microsoft.Extensions.Options.Options.Create(_options),
            NullLogger<AuthService>.Instance);

    private void SetupUser(User user) =>
        _userRepository
            .Setup(repository => repository.GetUserByUserIdAsync(
                user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private void SetupSessionLookup(string token, UserSession session) =>
        _sessionRepository
            .Setup(repository => repository.GetByTokenHashAsync(
                _refreshTokenService.HashToken(token),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

    private UserSession CreateSession(User user, string token) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            RefreshTokenHash = _refreshTokenService.HashToken(token),
            TokenFamilyId = Guid.NewGuid(),
            CreatedAtUtc = NowUtc.AddDays(-1),
            ExpiresAtUtc = NowUtc.AddDays(_options.RefreshTokenLifetimeDays - 1),
            LastUsedAtUtc = NowUtc.AddMinutes(-1)
        };

    private static User CreateUser(
        int id = 1,
        string userName = "admin",
        bool active = true,
        string password = "Password123!") =>
        new()
        {
            UserId = id,
            UserName = userName,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = active,
            PersonId = 10,
            SecurityStamp = "stamp-1"
        };

    private static LoginRequestDto Request(
        string userName = "admin",
        string password = "Password123!") =>
        new() { UserName = userName, Password = password };

    /// <summary>
    /// Atomic in-memory store used to exercise the single-winner rotation policy.
    /// The production guarantee comes from the SQL conditional UPDATE; this fake
    /// reproduces the same contract for a process-local concurrency test.
    /// </summary>
    private sealed class InMemorySessionRepository : IUserSessionRepository
    {
        private readonly object _gate = new();
        private readonly List<UserSession> _sessions = [];

        public IReadOnlyList<UserSession> Sessions
        {
            get
            {
                lock (_gate)
                {
                    return _sessions.ToList();
                }
            }
        }

        public Task<UserSession?> GetByTokenHashAsync(
            string tokenHash, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(
                    _sessions.FirstOrDefault(
                        session => session.RefreshTokenHash == tokenHash));
            }
        }

        public Task<bool> TryConsumeAsync(
            Guid sessionId,
            DateTime nowUtc,
            Guid replacedBySessionId,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var session = _sessions.FirstOrDefault(item => item.Id == sessionId);

                if (session is null ||
                    session.RevokedAtUtc is not null ||
                    session.ExpiresAtUtc <= nowUtc)
                {
                    return Task.FromResult(false);
                }

                session.RevokedAtUtc = nowUtc;
                session.RevocationReason = SessionRevocationReasons.Rotated;
                session.ReplacedBySessionId = replacedBySessionId;

                return Task.FromResult(true);
            }
        }

        public Task AddAsync(
            UserSession session, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _sessions.Add(session);
            }

            return Task.CompletedTask;
        }

        public Task<int> RevokeFamilyAsync(
            Guid tokenFamilyId,
            DateTime nowUtc,
            string reason,
            CancellationToken cancellationToken = default) =>
            RevokeWhere(
                session => session.TokenFamilyId == tokenFamilyId,
                nowUtc,
                reason);

        public Task<int> RevokeAllForUserAsync(
            int userId,
            DateTime nowUtc,
            string reason,
            CancellationToken cancellationToken = default) =>
            RevokeWhere(
                session => session.UserId == userId,
                nowUtc,
                reason);

        public Task<bool> HasActiveSessionInFamilyAsync(
            Guid tokenFamilyId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(
                    _sessions.Any(session =>
                        session.TokenFamilyId == tokenFamilyId &&
                        session.RevokedAtUtc is null &&
                        session.ExpiresAtUtc > nowUtc));
            }
        }

        public Task<int> RevokeByTokenHashAsync(
            string tokenHash,
            DateTime nowUtc,
            string reason,
            CancellationToken cancellationToken = default) =>
            RevokeWhere(
                session => session.RefreshTokenHash == tokenHash,
                nowUtc,
                reason);

        private Task<int> RevokeWhere(
            Func<UserSession, bool> predicate,
            DateTime nowUtc,
            string reason)
        {
            lock (_gate)
            {
                var revoked = 0;

                foreach (var session in _sessions.Where(predicate))
                {
                    if (session.RevokedAtUtc is not null)
                    {
                        continue;
                    }

                    session.RevokedAtUtc = nowUtc;
                    session.RevocationReason = reason;
                    revoked++;
                }

                return Task.FromResult(revoked);
            }
        }
    }
}
