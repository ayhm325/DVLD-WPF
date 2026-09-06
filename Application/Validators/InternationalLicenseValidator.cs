using Application.Common.Results;

namespace Application.Validators;

public static class InternationalLicenseValidator
{
    public static Result ValidateId(int id) =>
    id > 0
    ? Result.Success()
    : Result.ValidationFailure(
    "Invalid international license ID.");

    public static Result ValidateDriverId(int driverId) =>
    driverId > 0
        ? Result.Success()
        : Result.ValidationFailure(
            "Invalid driver ID.");

    public static Result ValidateApplicationId(int applicationId) =>
        applicationId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid application ID.");

    public static Result ValidateLocalLicenseId(int localLicenseId) =>
        localLicenseId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid local license ID.");
}
