using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public sealed class LicenseClassService : ILicenseClassService
{
    private readonly ILicenseClassRepository _licenseClassRepository;
    private readonly IUnitOfWork? _unitOfWork;

    public LicenseClassService(
        ILicenseClassRepository licenseClassRepository,
        IUnitOfWork unitOfWork)
    {
        _licenseClassRepository =
            licenseClassRepository
            ?? throw new ArgumentNullException(nameof(licenseClassRepository));
        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public LicenseClassService(
        ILicenseClassRepository licenseClassRepository)
    {
        _licenseClassRepository =
            licenseClassRepository
            ?? throw new ArgumentNullException(nameof(licenseClassRepository));
        _unitOfWork = null;
    }

    public async Task<Result<List<LicenseClassDto>>>
        GetAllLicenseClassesAsync(CancellationToken cancellationToken = default)
    {
        var licenseClasses =
            await _licenseClassRepository
                .GetAllLicenseClassAsync(cancellationToken);

        return Result<List<LicenseClassDto>>.Success(
            licenseClasses
                .Select(MapToDto)
                .ToList());
    }

    public async Task<Result<LicenseClassDto>>
        GetLicenseClassByIdAsync(int id, CancellationToken cancellationToken = default)
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
                .GetLicenseClassByIdAsync(id, cancellationToken);

        return licenseClass is null
            ? Result<LicenseClassDto>.FromNotFound(
                "License class not found.")
            : Result<LicenseClassDto>.Success(
                MapToDto(licenseClass));
    }

    public async Task<Result> UpdateLicenseClassAsync(
        int id,
        LicenseClassDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation =
            LicenseClassValidator.ValidateUpdate(id, dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var licenseClass =
            await _licenseClassRepository
                .GetForUpdateAsync(id, cancellationToken);

        if (licenseClass is null)
        {
            return Result.NotFound(
                "License class not found.");
        }

        bool hasChanges =
            licenseClass.ClassName != dto.LicenseClassName.Trim() ||
            licenseClass.ClassDescription != dto.LicenseClassDescription.Trim() ||
            licenseClass.MinimumAllowedAge != dto.MinAllowedAge ||
            licenseClass.DefaultValidityLength != dto.DefaultValidityLength ||
            licenseClass.ClassFees != dto.LicenseClassFees;

        if (!hasChanges)
        {
            return Result.Success();
        }

        licenseClass.ClassName = dto.LicenseClassName.Trim();
        licenseClass.ClassDescription = dto.LicenseClassDescription.Trim();
        licenseClass.MinimumAllowedAge = dto.MinAllowedAge;
        licenseClass.DefaultValidityLength = dto.DefaultValidityLength;
        licenseClass.ClassFees = dto.LicenseClassFees;

        if (_unitOfWork is null)
        {
            return Result.Failure("Unit of work is not configured.");
        }

        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saved > 0
            ? Result.Success()
            : Result.Failure("Failed to update license class.");
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
