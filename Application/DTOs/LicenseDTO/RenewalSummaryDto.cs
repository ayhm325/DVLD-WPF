namespace Application.DTOs.LicenseDTO;

public sealed class RenewalSummaryDto
{
    public int TotalExpired { get; set; }
    public int EligibleForRenewal { get; set; }
    public int IneligibleForRenewal { get; set; }
}
