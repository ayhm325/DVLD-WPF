using Application.Common.Results;
using Application.DTOs.ApplicationDTO;

namespace Application.Validators;

public static class ApplicationValidator
{
    public static Result ValidateCreate(CreateApplicationDto? dto)
    {
        if (dto is null)
            return Result.Failure("Application data is required.");

        var errors = new List<string>();

        if (dto.ApplicantPersonID <= 0)
            errors.Add("A valid applicant person is required.");

        if (dto.ApplicationTypeID <= 0)
            errors.Add("A valid application type is required.");

        return BuildResult(errors);
    }

    public static Result ValidateUpdate(UpdateApplicationDto? dto)
    {
        if (dto is null)
            return Result.Failure("Application data is required.");

        var errors = new List<string>();

        if (dto.ApplicationID <= 0)
            errors.Add("A valid application ID is required.");

        if (dto.ApplicationTypeID <= 0)
            errors.Add("A valid application type is required.");

        return BuildResult(errors);
    }

    public static Result ValidateId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid application ID.");

    private static Result BuildResult(List<string> errors) =>
        errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(string.Join(Environment.NewLine, errors));
}
