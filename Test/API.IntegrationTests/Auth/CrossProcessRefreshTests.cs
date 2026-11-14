using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using Application.Interfaces;
using Application.Services;

using Domain.Entities;
using Domain.Enums;

using Dvld.TestSupport;

using Infrastructure;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace API.IntegrationTests.Auth;

/// <summary>
/// AUTH-01-CROSS-PROCESS: two independent API OS processes share one isolated,
/// ownership-verified SQL database and race on the same refresh token.
/// </summary>
public sealed class CrossProcessRefreshTests
    : IClassFixture<SharedSqlDatabaseFixture>
{
    private static readonly IRefreshTokenService TokenService = new RefreshTokenService();

    private readonly SharedSqlDatabaseFixture _database;

    public CrossProcessRefreshTests(SharedSqlDatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task ConcurrentRefresh_AcrossTwoApiProcesses_ExactlyOneRotationWins()
    {
        var userName = "cross.race";
        var userId = await SeedUserAsync(userName);
        var rawToken = TokenService.CreateToken();
        var familyId = Guid.NewGuid();
        var absoluteExpiry = DateTime.UtcNow.AddDays(10);

        await SeedSessionAsync(userId, rawToken, familyId, absoluteExpiry);

        await using var hostA = await ApiHost.StartAsync(_database.ConnectionString, graceSeconds: 30);
        await using var hostB = await ApiHost.StartAsync(_database.ConnectionString, graceSeconds: 30);

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> RefreshAsync(ApiHost host)
        {
            await gate.Task;
            return await host.RefreshAsync(rawToken);
        }

        var first = RefreshAsync(hostA);
        var second = RefreshAsync(hostB);
        gate.SetResult();

        var responses = await Task.WhenAll(first, second);

        var statuses = responses.Select(response => (int)response.StatusCode)
            .OrderBy(status => status)
            .ToArray();

        Assert.Equal(new[] { 200, 409 }, statuses);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);

        var winner = responses.Single(response => response.StatusCode == HttpStatusCode.OK);

        using var body = JsonDocument.Parse(
            await winner.Content.ReadAsStringAsync());

        Assert.True(body.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(body.RootElement.TryGetProperty("refreshToken", out _));

        var rotatedCookie = ReadRefreshCookie(winner);
        Assert.False(string.IsNullOrWhiteSpace(rotatedCookie));
        Assert.NotEqual(rawToken, rotatedCookie);

        await using var verify = _database.CreateContext();

        var family = await verify.UserSessions
            .AsNoTracking()
            .Where(session => session.TokenFamilyId == familyId)
            .ToListAsync();

        Assert.Equal(2, family.Count);
        Assert.Single(family, session => session.RevokedAtUtc is null);

        var consumed = family.Single(session => session.RevokedAtUtc is not null);
        var replacement = family.Single(session => session.RevokedAtUtc is null);

        Assert.Equal(SessionRevocationReasons.Rotated, consumed.RevocationReason);
        Assert.Equal(replacement.Id, consumed.ReplacedBySessionId);
        Assert.Equal(absoluteExpiry, replacement.ExpiresAtUtc);
    }

    [Fact]
    public async Task ReuseAfterGrace_AcrossProcesses_RevokesFamilyForBothHosts()
    {
        var userName = "cross.reuse";
        var userId = await SeedUserAsync(userName);
        var rawToken = TokenService.CreateToken();
        var familyId = Guid.NewGuid();

        await SeedSessionAsync(userId, rawToken, familyId, DateTime.UtcNow.AddDays(10));

        await using var hostA = await ApiHost.StartAsync(_database.ConnectionString, graceSeconds: 0);
        await using var hostB = await ApiHost.StartAsync(_database.ConnectionString, graceSeconds: 0);

        var rotation = await hostA.RefreshAsync(rawToken);
        Assert.Equal(HttpStatusCode.OK, rotation.StatusCode);

        var replacementToken = ReadRefreshCookie(rotation);
        Assert.False(string.IsNullOrWhiteSpace(replacementToken));

        // Replay the consumed token through the other process, beyond grace.
        var reuse = await hostB.RefreshAsync(rawToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // The legitimate replacement is dead too: family revocation is shared state.
        var replacementAttempt = await hostA.RefreshAsync(replacementToken!);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementAttempt.StatusCode);

        await using var verify = _database.CreateContext();

        var family = await verify.UserSessions
            .AsNoTracking()
            .Where(session => session.TokenFamilyId == familyId)
            .ToListAsync();

        Assert.Equal(2, family.Count);
        Assert.All(family, session => Assert.NotNull(session.RevokedAtUtc));
        Assert.Contains(
            family,
            session => session.RevocationReason == SessionRevocationReasons.ReuseDetected);
    }

    private async Task<int> SeedUserAsync(string userName)
    {
        await using var context = _database.CreateContext();

        var country = new Country { CountryName = "Country-" + userName };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var person = new Person
        {
            NationalNo = "N" + Guid.NewGuid().ToString("N")[..9],
            FirstName = "Cross",
            SecondName = "Process",
            LastName = userName,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = Gender.Male,
            Address = "Test Address",
            Phone = "0790000000",
            NationalityCountryID = country.CountryId
        };

        context.People.Add(person);
        await context.SaveChangesAsync();

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = userName,
            Password = "not-a-real-hash",
            IsActive = true,
            Role = UserRole.Staff
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.UserId;
    }

    private async Task SeedSessionAsync(
        int userId,
        string rawToken,
        Guid familyId,
        DateTime absoluteExpiry)
    {
        var now = DateTime.UtcNow;

        await using var context = _database.CreateContext();

        context.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = TokenService.HashToken(rawToken),
            TokenFamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = absoluteExpiry,
            LastUsedAtUtc = now
        });

        await context.SaveChangesAsync();
    }

    private static string? ReadRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        var cookie = values
            .Select(value => value.Split(';')[0])
            .FirstOrDefault(value => value.StartsWith("dvld_rt=", StringComparison.Ordinal));

        return cookie is null ? null : cookie["dvld_rt=".Length..];
    }

    private sealed class ApiHost : IAsyncDisposable
    {
        private readonly Process _process;
        private readonly StringBuilder _log;
        private readonly HttpClient _client;

        private ApiHost(Process process, HttpClient client, StringBuilder log)
        {
            _process = process;
            _client = client;
            _log = log;
        }

        public static async Task<ApiHost> StartAsync(
            string connectionString,
            int graceSeconds)
        {
            var port = GetFreePort();
            var apiAssembly = typeof(Program).Assembly.Location;

            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetDirectoryName(apiAssembly)!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("exec");
            startInfo.ArgumentList.Add(apiAssembly);

            startInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
            startInfo.Environment["ConnectionStrings__DVLDConnection"] = connectionString;
            startInfo.Environment["Jwt__Issuer"] = "DVLD.Api";
            startInfo.Environment["Jwt__Audience"] = "DVLD.Client";
            startInfo.Environment["Jwt__SecretKey"] =
                "cross-process-test-signing-key-0123456789abcdef";
            startInfo.Environment["Jwt__ExpirationMinutes"] = "15";
            startInfo.Environment["AuthSession__RotationGraceSeconds"] =
                graceSeconds.ToString();
            startInfo.Environment["LicenseExpiration__IsEnabled"] = "false";
            startInfo.Environment["NoShowProcessor__IsEnabled"] = "false";

            var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the DVLD API process.");

            var log = new StringBuilder();
            var logGate = new object();

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    lock (logGate) log.AppendLine(args.Data);
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    lock (logGate) log.AppendLine(args.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var client = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{port}"),
                Timeout = TimeSpan.FromSeconds(60)
            };

            var deadline = DateTime.UtcNow.AddSeconds(60);

            while (DateTime.UtcNow < deadline)
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"The API process exited during startup (exit code {process.ExitCode}).{Environment.NewLine}{log}");
                }

                try
                {
                    using var probe = await client.GetAsync("/api/Auth/me");

                    if (probe.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.OK)
                    {
                        return new ApiHost(process, client, log);
                    }
                }
                catch (HttpRequestException)
                {
                    // Not listening yet.
                }
                catch (TaskCanceledException)
                {
                    // Startup probe timed out; retry until the deadline.
                }

                await Task.Delay(200);
            }

            process.Kill(entireProcessTree: true);

            throw new TimeoutException(
                $"The API process did not become ready within 60 seconds.{Environment.NewLine}{log}");
        }

        public async Task<HttpResponseMessage> RefreshAsync(string refreshToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post, "/api/Auth/refresh");

            request.Headers.Add("Cookie", $"dvld_rt={refreshToken}");
            request.Headers.Add("X-CSRF-Protection", "1");

            return await _client.SendAsync(request);
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }

                await _process.WaitForExitAsync();
            }
            finally
            {
                _process.Dispose();
            }
        }

        private static int GetFreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}

/// <summary>
/// Creates one isolated, ownership-verified SQL database shared by both API processes.
/// </summary>
public sealed class SharedSqlDatabaseFixture : IAsyncLifetime
{
    private SqlServerTestTarget? _target;
    private SqlServerTestDatabaseLease? _lease;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _target = SqlServerTestEnvironment.ReadConfigurationFromEnvironment();

        var databaseName = SqlServerTestEnvironment.BuildDatabaseName(
            "DVLD_IntegrationTests_Api_");

        await using (var admin = new SqlConnection(_target.AdminConnectionString))
        {
            await admin.OpenAsync();
            _lease = await SqlServerTestEnvironment.CreateDatabaseAsync(
                admin, _target, databaseName);
        }

        ConnectionString = SqlServerTestEnvironment.BuildDatabaseConnectionString(
            _target, _lease.DatabaseName);

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public DVLDDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    public async Task DisposeAsync()
    {
        if (_target is null || _lease is null)
        {
            return;
        }

        await using var admin = new SqlConnection(_target.AdminConnectionString);
        await admin.OpenAsync();

        await SqlServerTestEnvironment.DropOwnedDatabaseAsync(admin, _target, _lease);
    }
}
