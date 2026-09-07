namespace DVLD.Contracts.License;

public sealed class LicenseResponse
{
    public int LicenseId { get; init; }

    public int ApplicationId { get; init; }

    public int DriverId { get; init; }

    public string? DriverName { get; init; }

    public int LicenseClassId { get; init; }

    public string? LicenseClassName { get; init; }

    public DateTime IssueDate { get; init; }

    public DateTime ExpirationDate { get; init; }

    public string? Notes { get; init; }

    public decimal PaidFees { get; init; }

    public bool IsActive { get; init; }

    public byte IssueReason { get; init; }

    public string? IssueReasonText { get; init; }

    public int CreatedByUserId { get; init; }

    public string? CreatedByUserName { get; init; }
}