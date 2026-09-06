using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace Presentation.Services;

internal class ApiHostService : IApiHostService
{
    private const string ApiUrl = "http://localhost:5260";
    private Process? _apiProcess;

    public async Task EnsureApiRunningAsync(
        CancellationToken cancellationToken = default)
    {
        if (await IsApiRunningAsync(cancellationToken))
            return;

        StartApi();

        await WaitForApiAsync(cancellationToken);
    }

    private async Task<bool> IsApiRunningAsync(
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        try
        {
            await client.GetAsync(ApiUrl, cancellationToken);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private void StartApi()
    {
        var projectPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                @"..\..\..\..\API\DVLD.Api\DVLD.Api.csproj"));

        var apiDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException(
                "Could not locate the DVLD API project.");

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException(
                "DVLD API project was not found.",
                projectPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --launch-profile http",
            WorkingDirectory = apiDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _apiProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Could not start the DVLD API.");
    }

    private async Task WaitForApiAsync(
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 40;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_apiProcess is { HasExited: true })
            {
                throw new InvalidOperationException(
                    "The DVLD API process stopped unexpectedly.");
            }

            if (await IsApiRunningAsync(cancellationToken))
                return;

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException(
            "The DVLD API did not become ready within 10 seconds.");
    }
}