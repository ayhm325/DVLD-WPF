namespace DVLD.Contracts.Application;

public sealed class UpdateApplicationRequest
{
    public int ApplicationId { get; init; }

    public int ApplicationTypeId { get; init; }
}
