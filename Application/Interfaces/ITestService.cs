using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ITestService
    {
        Task<Result<TestDto>> GetByIdAsync(int id);

        Task<Result<List<TestDto>>> GetAllAsync();

        Task<Result<List<TestDto>>> GetByTestAppointmentIdAsync(int appointmentId);

        Task<Result<List<TestDto>>> GetByUserIdAsync(int userId);

        // فحوصات (تبقى bool)
        Task<bool> IsTestExistsAsync(int id);
        Task<bool> IsTestAlreadyTakenAsync(int appointmentId);

        // أوامر (Commands)
        Task<Result<int>> AddAsync(TestDto dto);
        Task<Result> UpdateAsync(TestDto dto);
        Task<Result> DeleteAsync(int id);
    }
}