using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class LicenseClassService : ILicenseClassService
    {
        private readonly LicenseClassRepository _licenseClassRepository;

        public LicenseClassService(LicenseClassRepository licenseClassRepository)
        {
            _licenseClassRepository = licenseClassRepository;
        }


        // ================= GET ALL =================
        public async Task<List<LicenseClassDto>> GetAllLicenseClassesAsync()
        {
            var licenseClasses = await _licenseClassRepository.GetAllLicenseClassAsync();
            return [.. licenseClasses.Select(MapToDto)];
        }

        // ================= GET BY ID =================
        public async Task<LicenseClassDto?> GetLicenseClassByIdAsync(int id)
        {
            var licenseClass = await _licenseClassRepository.GetLicenseClassByIdAsync(id);
            return licenseClass != null ? MapToDto(licenseClass) : null;
        }

        // ================= MAPPING =================
        private LicenseClassDto MapToDto(LicenseClass licenseClass)
        {
            return new LicenseClassDto
            {
                LicenseClassId = licenseClass.LicenseClassID,
                LicenseClassName = licenseClass.ClassName,
                LicenseClassDescription = licenseClass.ClassDescription,
                MinAllowedAge = licenseClass.MinimumAllowedAge,
                DefaultValidityLength = licenseClass.DefaultValidityLength,
                LicenseClassFees = licenseClass.ClassFees
            };
        }

    }
}
