namespace DVLD.Contracts.LicenseIssuance;

public sealed record IssueFirstLicenseRequest(
    int LocalApplicationId,
    string? Notes);