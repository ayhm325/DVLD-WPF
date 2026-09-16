
namespace DVLD.Contracts.Driver;

public sealed class DriverListResponse
{
    public int DriverId { get; init; }
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string NationalNo { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public DateTime CreatedDate { get; init; }
    public int ActiveLicenses { get; init; }
}