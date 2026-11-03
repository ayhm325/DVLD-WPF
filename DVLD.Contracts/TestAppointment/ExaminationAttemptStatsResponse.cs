namespace DVLD.Contracts.TestAppointment;

public sealed record ExaminationAttemptStatsResponse(
    int LocalDrivingLicenseApplicationId,
    int TestTypeId,
    int TotalAttempts,
    int PassedAttempts,
    int FailedAttempts,
    int NoShowAttempts,
    int NextTrialNumber);
