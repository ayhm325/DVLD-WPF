namespace DVLD.Contracts.InternationalLicense;

public sealed class InternationalLicenseListResponse
{
    public int InternationalLicenseId { get; init; }
    public int ApplicationId { get; init; }
    public int DriverId { get; init; }
    public int IssuedUsingLocalLicenseId { get; init; }
    public int PersonId { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime ExpirationDate { get; init; }
    public bool IsActive { get; init; }
}