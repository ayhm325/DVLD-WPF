using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ApplicationTypeService : IApplicationTypeService
    {
        private readonly ApplicationTypeRespository _applicationTypeRespository;

        public ApplicationTypeService(ApplicationTypeRespository applicationTypeRespository)
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
            var appTypes = await _applicationTypeRespository.GetApplicationTypeByIdAsync(id);
            return appTypes == null ? null : MapToDto(appTypes);
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateApplicationTypeAsync(int id, ApplicationTypeDto dto)
        {
            var appTypes = await _applicationTypeRespository.GetApplicationTypeByIdAsync(id);
            if (appTypes is null) return false;

            appTypes.ApplicationTypeTitle = dto.ApplicationTypeTitle;
            appTypes.ApplicationFees = dto.ApplicationTypeFees;

            return await _applicationTypeRespository.UpdateApplicationTypeAsync(appTypes);

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
