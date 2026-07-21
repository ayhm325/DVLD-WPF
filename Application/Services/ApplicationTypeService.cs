using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ApplicationTypeService : IApplicationTypeService
    {
        private readonly IApplicationTypeRepository _applicationTypeRespository;

        public ApplicationTypeService(IApplicationTypeRepository applicationTypeRespository)
        {
            _applicationTypeRespository = applicationTypeRespository;
        }

        // ================= GET ALL =================
        public async Task<Result<List<ApplicationTypeDto>>> GetAllApplicationTypesAsync()
        {
            var appTypes = await _applicationTypeRespository.GetAllApplicationTypesAsync();

            return Result<List<ApplicationTypeDto>>.Success(
                [.. appTypes.Select(MapToDto)]);
        }

        // ================= GET BY ID =================
        public async Task<Result<ApplicationTypeDto>> GetApplicationTypeByIdAsync(int id)
        {
            var appType = await _applicationTypeRespository.GetApplicationTypeByIdAsync(id);

            if (appType == null)
                return Result<ApplicationTypeDto>.Fail("نوع الطلب غير موجود.");

            return Result<ApplicationTypeDto>.Success(MapToDto(appType));
        }

        // ================= UPDATE =================
        public async Task<Result> UpdateApplicationTypeAsync(int id, ApplicationTypeDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات نوع الطلب مطلوبة.");

            var appType = await _applicationTypeRespository.GetApplicationTypeByIdAsync(id);

            if (appType is null)
                return Result.Failure("نوع الطلب غير موجود.");

            appType.ApplicationTypeTitle = dto.ApplicationTypeTitle;
            appType.ApplicationFees = dto.ApplicationTypeFees;

            var isSuccess = await _applicationTypeRespository.UpdateApplicationTypeAsync(appType);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث نوع الطلب.");
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