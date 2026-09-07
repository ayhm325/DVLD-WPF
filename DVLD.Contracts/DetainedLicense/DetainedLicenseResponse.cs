namespace DVLD.Contracts.DetainedLicense;

public sealed class DetainedLicenseResponse
{
    public int DetainId { get; init; }

    public int LicenseId { get; init; }

    public int PersonId { get; init; }

    public string? NationalNo { get; init; }

    public string? FullName { get; init; }

    public DateTime DetainDate { get; init; }

    public decimal FineFees { get; init; }

    public int CreatedByUserId { get; init; }

    public string? CreatedByUserName { get; init; }

    public bool IsReleased { get; init; }

    public DateTime? ReleaseDate { get; init; }

    public int? ReleasedByUserId { get; init; }

    public int? ReleaseApplicationId { get; init; }
}