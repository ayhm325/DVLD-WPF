namespace DVLD.Contracts.Driver;

public sealed class UpdateDriverRequest
{
    public int DriverId { get; init; }

    public int PersonId { get; init; }
}