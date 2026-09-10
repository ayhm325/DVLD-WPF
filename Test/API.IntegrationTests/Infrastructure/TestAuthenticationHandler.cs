using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace API.IntegrationTests.Infrastructure;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                "X-Test-User-Id",
                out var userIdHeader))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        if (!int.TryParse(userIdHeader, out var userId))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Invalid test user id."));
        }

        var username =
            Request.Headers["X-Test-Username"].FirstOrDefault()
            ?? "testuser";

        var fullName =
            Request.Headers["X-Test-FullName"].FirstOrDefault()
            ?? "Test User";

        var role =
            Request.Headers["X-Test-Role"].FirstOrDefault()
            ?? "Staff";

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new Claim(
                ClaimTypes.Name,
                username),

            new Claim(
                "FullName",
                fullName),

            new Claim(
                ClaimTypes.Role,
                role)
        };

        var identity = new ClaimsIdentity(
            claims,
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            Scheme.Name);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}