using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using Infrastructure.Repositories;


namespace Application.Services
{
    public class ApplicationTypeService : IApplicationTypeService
    {
        private readonly ApplicationTypeRepository _applicationTypeRespository;

        public ApplicationTypeService(ApplicationTypeRepository applicationTypeRespository)
        {
            _applicationTypeRespository = applicationTypeRespository;
        }

        // ================= GET ALL =================
        public async Task<List<ApplicationTypeDto>> GetAllApplicationTypesAsync()
        {
            var appTypes = await _applicationTypeRespository.GetAllApplicationTypesAsync();
            return [.. appTypes.Select(MapToDto)];
        }

        // ================= GET BY ID =================
        public async Task<ApplicationTypeDto?> GetApplicationTypeByIdAsync(int id)
        {
            var appType = await _applicationTypeRespository.GetApplicationTypeByIdAsync(id);
            return appType == null ? null : MapToDto(appType);
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateApplicationTypeAsync(int id, ApplicationTypeDto dto)
        {
            var appType = await _applicationTypeRespository.GetApplicationTypeByIdAsync(id);
            if (appType is null) return false;

            appType.ApplicationTypeTitle = dto.ApplicationTypeTitle;
            appType.ApplicationFees = dto.ApplicationTypeFees;

            return await _applicationTypeRespository.UpdateApplicationTypeAsync(appType);

        }

        // ================= MAPPING =================
        private ApplicationTypeDto MapToDto(ApplicationType apptype)
        {
            return new ApplicationTypeDto
            {
                ApplicationTypeId = apptype.ApplicationTypeId,
                ApplicationTypeTitle = apptype.ApplicationTypeTitle,
                ApplicationTypeFees = apptype.ApplicationFees
            };
        }


    }
}
