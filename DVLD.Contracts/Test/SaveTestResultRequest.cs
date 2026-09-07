namespace DVLD.Contracts.Test;

public sealed record SaveTestResultRequest(
    int TestAppointmentId,
    bool TestResult,
    string? Notes);