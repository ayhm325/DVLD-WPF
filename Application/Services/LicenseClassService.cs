using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class LicenseClassService : ILicenseClassService
{
    private readonly ILicenseClassRepository _licenseClassRepository;

    public LicenseClassService(ILicenseClassRepository licenseClassRepository)
    {
        _licenseClassRepository = licenseClassRepository ?? throw new ArgumentNullException(nameof(licenseClassRepository));
    }

    // GET ALL
    public async Task<Result<List<LicenseClassDto>>> GetAllLicenseClassesAsync()
    {
        var licenseClasses = await _licenseClassRepository.GetAllLicenseClassAsync();
        return Result<List<LicenseClassDto>>.Success([.. licenseClasses.Select(MapToDto)]);
    }

    // GET BY ID
    public async Task<Result<LicenseClassDto>> GetLicenseClassByIdAsync(int id)
    {
        var validation = LicenseClassValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<LicenseClassDto>.FromFailure(validation.Error);

        var licenseClass = await _licenseClassRepository.GetLicenseClassByIdAsync(id);
        if (licenseClass is null)
            return Result<LicenseClassDto>.FromFailure("License class not found.");

        return Result<LicenseClassDto>.Success(MapToDto(licenseClass));
    }

    // MAPPING
    private static LicenseClassDto MapToDto(LicenseClass licenseClass)
    {
        return new LicenseClassDto
        {
            LicenseClassID = licenseClass.LicenseClassID,
            LicenseClassName = licenseClass.ClassName,
            LicenseClassDescription = licenseClass.ClassDescription,
            MinAllowedAge = licenseClass.MinimumAllowedAge,
            DefaultValidityLength = licenseClass.DefaultValidityLength,
            LicenseClassFees = licenseClass.ClassFees
        };
    }
}