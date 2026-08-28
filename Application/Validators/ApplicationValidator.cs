using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Domain.Enums;

namespace Application.Validators;

public static class ApplicationValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(
        CreateApplicationDto? dto)
    {
        if (dto is null)
            return Result.Failure(
                "Application data is required.");

        var errors = new List<string>();

        ValidateCommonFields(
            dto.ApplicantPersonID,
            dto.ApplicationTypeID,
            dto.ApplicationStatus,
            dto.ApplicationDate,
            dto.LastStatusDate,
            dto.PaidFees,
            dto.CreatedByUserID,
            errors);

        return CreateResult(errors);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(
        UpdateApplicationDto? dto)
    {
        if (dto is null)
            return Result.Failure(
                "Application data is required.");

        var errors = new List<string>();

        if (dto.ApplicationID <= 0)
        {
            errors.Add(
                "A valid application ID is required.");
        }

        ValidateCommonFields(
            dto.ApplicantPersonID,
            dto.ApplicationTypeID,
            dto.ApplicationStatus,
            dto.ApplicationDate,
            dto.LastStatusDate,
            dto.PaidFees,
            dto.CreatedByUserID,
            errors);

        return CreateResult(errors);
    }


    // =========================================================
    // ID
    // =========================================================

    public static Result ValidateId(int id)
    {
        return id > 0
            ? Result.Success()
            : Result.Failure(
                "Invalid application ID.");
    }


    // =========================================================
    // COMMON VALIDATION
    // =========================================================

    private static void ValidateCommonFields(
        int applicantPersonId,
        int applicationTypeId,
        AppStatus applicationStatus,
        DateTime applicationDate,
        DateTime lastStatusDate,
        decimal paidFees,
        int createdByUserId,
        List<string> errors)
    {
        if (applicantPersonId <= 0)
        {
            errors.Add(
                "A valid applicant person is required.");
        }


        if (applicationTypeId <= 0)
        {
            errors.Add(
                "A valid application type is required.");
        }


        if (!Enum.IsDefined(applicationStatus))
        {
            errors.Add(
                "Invalid application status.");
        }


        if (applicationDate == default)
        {
            errors.Add(
                "Application date is required.");
        }


        if (lastStatusDate == default)
        {
            errors.Add(
                "Last status date is required.");
        }


        if (paidFees < 0)
        {
            errors.Add(
                "Paid fees cannot be negative.");
        }


        if (createdByUserId <= 0)
        {
            errors.Add(
                "A valid creating user is required.");
        }
    }


    // =========================================================
    // RESULT
    // =========================================================

    private static Result CreateResult(
        List<string> errors)
    {
        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(
                string.Join(
                    Environment.NewLine,
                    errors));
    }
}