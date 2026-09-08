namespace DVLD.Contracts.TestType;

public sealed class TestTypeResponse
{
    public int TestTypeId { get; init; }

    public string TestTypeTitle { get; init; } =
        string.Empty;

    public string TestTypeDescription { get; init; } =
        string.Empty;

    public decimal TestTypeFees { get; init; }
}