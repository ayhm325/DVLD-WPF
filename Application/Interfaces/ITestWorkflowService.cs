using Application.Common.Results;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITestWorkflowService
{
    // =========================================================
    // SCHEDULING
    // =========================================================

    Task<Result> CanScheduleTestAsync(
        int localAppId,
        TestTypeEnum testType);


    // =========================================================
    // NEXT TEST
    // =========================================================

    Task<Result<TestTypeEnum>> GetNextTestTypeAsync(
        int localAppId);


    // =========================================================
    // TAKE TEST
    // =========================================================

    Task<Result> CanTakeTestAsync(
        int testAppointmentId);


    // =========================================================
    // COMPLETION
    // =========================================================

    Task<bool> HasPassedAllTestsAsync(
        int localAppId);
}