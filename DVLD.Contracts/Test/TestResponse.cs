namespace DVLD.Contracts.Test;

public sealed record TestResponse(
    int TestId,
    int TestAppointmentId,
    bool TestResult,
    string? Notes,
    int CreatedByUserId,
    string? CreatedByUserName,
    string? TestTypeName,
    DateTime? AppointmentDate);