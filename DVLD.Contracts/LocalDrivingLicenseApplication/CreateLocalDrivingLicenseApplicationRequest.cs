namespace DVLD.Contracts.LocalDrivingLicenseApplication;

public sealed class CreateLocalDrivingLicenseApplicationRequest
{
    public int ApplicantPersonId { get; init; }
    public int LicenseClassId { get; init; }
}