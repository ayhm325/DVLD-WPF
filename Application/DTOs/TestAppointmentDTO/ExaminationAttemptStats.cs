namespace Application.DTOs.TestAppointmentDTO;

public sealed record ExaminationAttemptStats(
    int LocalDrivingLicenseApplicationId,
    int TestTypeId,
    int TotalAttempts,
    int PassedAttempts,
    int FailedAttempts,
    int NoShowAttempts,
    int NextTrialNumber);
