using Application.Common.Results;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITestWorkflowService
{
    Task<Result> CanScheduleTestAsync(
        int localAppId,
        TestTypeEnum testType);

    Task<Result<TestTypeEnum>> GetNextTestTypeAsync(
        int localAppId);

    Task<Result> CanTakeTestAsync(
        int testAppointmentId);

    Task<bool> HasPassedAllTestsAsync(
        int localAppId);
}
