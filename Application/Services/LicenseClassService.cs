using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LicenseClassService : ILicenseClassService
    {
        private readonly ILicenseClassRepository _licenseClassRepository;

        public LicenseClassService(ILicenseClassRepository licenseClassRepository)
        {
            _licenseClassRepository = licenseClassRepository;
        }

        // ================= GET ALL =================
        public async Task<Result<List<LicenseClassDto>>> GetAllLicenseClassesAsync()
        {
            var licenseClasses = await _licenseClassRepository.GetAllLicenseClassAsync();

            return Result<List<LicenseClassDto>>.Success(
                [.. licenseClasses.Select(MapToDto)]);
        }

        // ================= GET BY ID =================
        public async Task<Result<LicenseClassDto>> GetLicenseClassByIdAsync(int id)
        {
            var licenseClass = await _licenseClassRepository.GetLicenseClassByIdAsync(id);

            if (licenseClass == null)
                return Result<LicenseClassDto>.Fail("فئة الرخصة غير موجودة.");

            return Result<LicenseClassDto>.Success(MapToDto(licenseClass));
        }

        // ================= MAPPING =================
        private LicenseClassDto MapToDto(LicenseClass licenseClass)
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
}