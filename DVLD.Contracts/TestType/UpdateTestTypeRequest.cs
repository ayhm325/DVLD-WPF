namespace DVLD.Contracts.TestType;

public sealed class UpdateTestTypeRequest
{
    public int TestTypeId { get; set; }

    public string TestTypeTitle { get; set; } =
        string.Empty;

    public string TestTypeDescription { get; set; } =
        string.Empty;

    public decimal TestTypeFees { get; set; }
}