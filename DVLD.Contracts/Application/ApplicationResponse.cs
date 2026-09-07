namespace DVLD.Contracts.Application;

public sealed class ApplicationResponse
{
    public int ApplicationId { get; init; }

    public int ApplicantPersonId { get; init; }

    public DateTime ApplicationDate { get; init; }

    public int ApplicationTypeId { get; init; }

    public string ApplicationStatus { get; init; } = string.Empty;

    public string StatusText { get; init; } = string.Empty;

    public DateTime LastStatusDate { get; init; }

    public decimal PaidFees { get; init; }

    public int CreatedByUserId { get; init; }

    public string CreatedByUserName { get; init; } = string.Empty;
}
