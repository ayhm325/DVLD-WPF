using Application.Common.Results;
using Domain.Enums;
using Domain.Entities;

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

    Task<Result> CanTakeTestAsync(TestAppointment appointment);

    Task<bool> HasPassedAllTestsAsync(
        int localAppId);
}
