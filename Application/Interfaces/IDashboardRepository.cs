using Application.DTOs;

namespace Application.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardDto> GetStatisticsAsync(DateOnly jordanToday, CancellationToken cancellationToken = default);
}
