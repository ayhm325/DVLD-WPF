using System.IdentityModel.Tokens.Jwt;
using Application.Common.Time;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Options;
using Domain.Enums;
using DVLD.Api.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.IntegrationTests.Security;

public sealed class JwtTokenServiceClockTests
{
    private sealed class FixedJordanClock(DateTime utcNow) : IJordanClock
    {
        public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        public DateTime Now => JordanTimeZoneProvider.ConvertUtcToJordan(UtcNow);
        public DateOnly Today => DateOnly.FromDateTime(Now);
    }

    private static IOptions<JwtOptions> CreateJwtOptions(int expirationMinutes = 60) =>
        Options.Create(new JwtOptions
        {
            Issuer = "DVLD.TestIssuer",
            Audience = "DVLD.TestAudience",
            SecretKey = "test-secret-key-0123456789-abcdef-0123456789",
            ExpirationMinutes = expirationMinutes
        });

    [Fact]
    public void Constructor_WhenClockIsNull_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new JwtTokenService(CreateJwtOptions(), null!));

        Assert.Equal("clock", ex.ParamName);
    }

    [Fact]
    public void GenerateToken_UsesClockUtcNowAndPreservesUtcKindEvenAcrossJordanMidnight()
    {
        // 2026-11-03 21:30:00Z UTC corresponds to 2026-11-04 00:30:00 Jordan local time.
        // JWT expiry MUST be anchored to UtcNow (22:30:00Z), never Jordan local wall-clock time.
        var fixedUtc = new DateTime(2026, 11, 3, 21, 30, 0, DateTimeKind.Utc);
        var clock = new FixedJordanClock(fixedUtc);
        var sut = new JwtTokenService(CreateJwtOptions(expirationMinutes: 60), clock);

        var result = sut.GenerateToken(new JwtTokenRequest
        {
            UserId = 42,
            PersonId = 100,
            UserName = "staff.jordan",
            FullName = "Jordan Staff",
            Role = UserRole.Staff,
            SecurityStamp = "stamp-clock"
        });

        var expectedExpiryUtc = new DateTime(2026, 11, 3, 22, 30, 0, DateTimeKind.Utc);

        Assert.Equal(DateTimeKind.Utc, result.ExpiresAtUtc.Kind);
        Assert.Equal(expectedExpiryUtc, result.ExpiresAtUtc);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal(expectedExpiryUtc, parsed.ValidTo);
    }
}
