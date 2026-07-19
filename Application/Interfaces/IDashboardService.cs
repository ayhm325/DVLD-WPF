using Application.DTOs;

public interface IDashboardService
{
    Task<DashboardDto> GetStatisticsAsync();
}