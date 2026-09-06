using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public sealed class LicenseClassService(
    ILicenseClassRepository licenseClassRepository)
    : ILicenseClassService
{
    private readonly ILicenseClassRepository _licenseClassRepository =
        licenseClassRepository
        ?? throw new ArgumentNullException(nameof(licenseClassRepository));

    public async Task<Result<List<LicenseClassDto>>>
        GetAllLicenseClassesAsync()
    {
        var licenseClasses =
            await _licenseClassRepository
                .GetAllLicenseClassAsync();

        return Result<List<LicenseClassDto>>.Success(
            licenseClasses
                .Select(MapToDto)
                .ToList());
    }

    public async Task<Result<LicenseClassDto>>
        GetLicenseClassByIdAsync(int id)
    {
        var validation =
            LicenseClassValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<LicenseClassDto>.FromValidationFailure(
                validation.Error);
        }

        var licenseClass =
            await _licenseClassRepository
                .GetLicenseClassByIdAsync(id);

        return licenseClass is null
            ? Result<LicenseClassDto>.FromNotFound(
                "License class not found.")
            : Result<LicenseClassDto>.Success(
                MapToDto(licenseClass));
    }

    private static LicenseClassDto MapToDto(
        LicenseClass licenseClass) =>
        new()
        {
            LicenseClassID = licenseClass.LicenseClassID,
            LicenseClassName = licenseClass.ClassName,
            LicenseClassDescription = licenseClass.ClassDescription,
            MinAllowedAge = licenseClass.MinimumAllowedAge,
            DefaultValidityLength = licenseClass.DefaultValidityLength,
            LicenseClassFees = licenseClass.ClassFees
        };
}