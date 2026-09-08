namespace DVLD.Contracts.LicenseReplacement;

public sealed record ReplaceLicenseRequest(
    int OldLicenseId,
    string ReplacementReason);