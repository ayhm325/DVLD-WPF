namespace Presentation.Services;

internal interface IApiHostService
{
    Task EnsureApiRunningAsync(CancellationToken cancellationToken = default);
}