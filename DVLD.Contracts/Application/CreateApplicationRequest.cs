namespace DVLD.Contracts.Application;

public sealed class CreateApplicationRequest
{
    public int ApplicantPersonId { get; init; }

    public int ApplicationTypeId { get; init; }
}
