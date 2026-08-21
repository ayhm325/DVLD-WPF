using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITestAppointmentService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<TestAppointmentDto>>
        GetByIdAsync(int id);

    Task<Result<List<TestAppointmentDto>>>
        GetAllAsync();

    Task<Result<List<TestAppointmentDto>>>
        GetByApplicationIdAsync(
            int applicationId);

    Task<Result<List<TestAppointmentDto>>>
        GetByTestTypeIdAsync(
            TestTypeEnum testType);

    Task<Result<List<TestAppointmentDto>>>
        GetByCreatedUserIdAsync(
            int userId);

    Task<Result<ScheduleTestDto>>
        GetScheduleInfoAsync(
            int testAppointmentId);


    // =========================================================
    // BUSINESS HELPERS
    // =========================================================

    Task<decimal>
        GetTestTypeFeesAsync(
            int testTypeId);

    Task<int>
        GetTrialCountAsync(
            int localAppId,
            int testTypeId);


    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool>
        HasConflictAsync(
            int testTypeId,
            DateTime dateTime);

    Task<bool>
        HasUserConflictAsync(
            int userId,
            DateTime dateTime);

    Task<bool>
        HasApplicationConflictAsync(
            int applicationId,
            DateTime dateTime);

    Task<bool>
        HasPassedAllTestsAsync(
            int appId);

    Task<bool>
        IsAppointmentAlreadyScheduledAsync(
            int localAppId,
            int testTypeId);


    // =========================================================
    // COMMANDS
    // =========================================================

    Task<Result>
        AddAsync(
            CreateTestAppointmentDto dto);

    Task<Result>
        UpdateAsync(
            UpdateTestAppointmentDto dto);

    Task<Result>
        DeleteAsync(
            int id);

    Task<Result>
        SaveTestResultAsync(
            SaveTestResultDto dto);
}