using Application.Common.Results;
using Domain.Enums;
using Domain.Entities;

namespace Application.Interfaces;

public interface ITestWorkflowService
{
    Task<Result> CanScheduleTestAsync(
        int localAppId,
        TestTypeEnum testType,
        CancellationToken cancellationToken = default);

    Task<Result<TestTypeEnum>> GetNextTestTypeAsync(
        int localAppId,
        CancellationToken cancellationToken = default);

    Task<Result> CanTakeTestAsync(
        int testAppointmentId,
        CancellationToken cancellationToken = default);

    Task<Result> CanTakeTestAsync(
        TestAppointment appointment,
        CancellationToken cancellationToken = default);

    Task<bool> HasPassedAllTestsAsync(
        int localAppId,
        CancellationToken cancellationToken = default);
}