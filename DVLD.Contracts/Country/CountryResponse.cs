namespace DVLD.Contracts.Country;

public sealed class CountryResponse
{
    public int CountryId { get; init; }

    public string CountryName { get; init; } = string.Empty;
}