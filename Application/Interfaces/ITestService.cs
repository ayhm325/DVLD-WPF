using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;

namespace Application.Interfaces;

public interface ITestService
{
    Task<Result<TestDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<List<TestDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<List<TestDto>>>
        GetByTestAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);

    Task<Result<List<TestDto>>>
        GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<Result<int>>
        AddAsync(SaveTestResultDto dto, CancellationToken cancellationToken = default);
}