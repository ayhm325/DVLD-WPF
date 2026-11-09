using Application.DTOs;
using Application.Interfaces;

namespace Application.Services;

public sealed class DashboardService(
    IDashboardRepository repository,
    IJordanClock clock) : IDashboardService
{
    private readonly IDashboardRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IJordanClock _clock =
        clock ?? throw new ArgumentNullException(nameof(clock));

    public Task<DashboardDto> GetStatisticsAsync(CancellationToken cancellationToken = default) =>
        _repository.GetStatisticsAsync(_clock.Today, cancellationToken);
}
