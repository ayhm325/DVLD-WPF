using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;

namespace Application.Interfaces;

public interface ITestService
{
    Task<Result<TestDto>> GetByIdAsync(int id);

    Task<Result<List<TestDto>>> GetAllAsync();

    Task<Result<List<TestDto>>>
        GetByTestAppointmentIdAsync(int appointmentId);

    Task<Result<List<TestDto>>>
        GetByUserIdAsync(int userId);

    Task<Result<int>>
        AddAsync(SaveTestResultDto dto);
}
