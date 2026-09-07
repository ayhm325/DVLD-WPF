namespace DVLD.Contracts.Application;

public sealed class ApplicationBasicInfoResponse
{
    public int ApplicantPersonId { get; init; }

    public int ApplicationId { get; init; }

    public string ApplicationStatus { get; init; } = string.Empty;

    public string StatusText { get; init; } = string.Empty;

    public decimal PaidFees { get; init; }

    public string? ApplicationTypeName { get; init; }

    public string? ApplicantFullName { get; init; }

    public DateTime ApplicationDate { get; init; }

    public DateTime LastStatusDate { get; init; }

    public string? CreatedByUserName { get; init; }
}
