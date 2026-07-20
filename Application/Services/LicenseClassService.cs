using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;


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
