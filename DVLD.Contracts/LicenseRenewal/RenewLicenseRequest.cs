namespace DVLD.Contracts.LicenseRenewal;

public sealed record RenewLicenseRequest(
    int OldLicenseId,
    string? Notes);