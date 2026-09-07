namespace DVLD.Contracts.ApplicationType;

public sealed class ApplicationTypeResponse
{
    public int ApplicationTypeId { get; init; }

    public string ApplicationTypeTitle { get; init; } = string.Empty;

    public decimal ApplicationTypeFees { get; init; }
}