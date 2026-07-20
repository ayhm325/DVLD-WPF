using Application.DTOs;
using Application.Interfaces;

namespace Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;


        public DashboardService(
            IDashboardRepository repository)
        {
            _repository = repository;
        }


        public async Task<DashboardDto> GetStatisticsAsync()
        {
            return await _repository.GetStatisticsAsync();
        }
    }
}