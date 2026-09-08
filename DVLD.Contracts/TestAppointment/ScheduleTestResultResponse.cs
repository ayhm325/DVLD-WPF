namespace DVLD.Contracts.TestAppointment;

public sealed class ScheduleTestResultResponse
{
    public int AppointmentId { get; init; }

    public int? RetakeTestApplicationId { get; init; }

    public int LocalDrivingLicenseApplicationId { get; init; }

    public string? LicenseClassName { get; init; }

    public string? FullName { get; init; }

    public int Trial { get; init; }

    public DateTime Date { get; init; }

    public decimal Fees { get; init; }

    public int TestTypeId { get; init; }

    public decimal RetakerFees { get; init; }

    public int TestId { get; init; }

    public bool Result { get; init; }

    public string? Notes { get; init; }
}