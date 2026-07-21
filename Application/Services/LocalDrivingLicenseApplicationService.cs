using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LocalDrivingLicenseApplicationService : ILocalDrivingLicenseApplicationService
    {
        private readonly ILocalDrivingLicenseApplicationRepository _repository;
        private readonly ILicenseRepository _licenseRepository;

        public LocalDrivingLicenseApplicationService(
            ILocalDrivingLicenseApplicationRepository repository,
            ILicenseRepository licenseRepository)
        {
            _repository = repository;
            _licenseRepository = licenseRepository;
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetAllLocalDrivingLicenseApplicationsAsync()
        {
            var entities = await _repository.GetAllAsync();
            var dtoList = new List<LocalDrivingLicenseApplicationListDto>();

            foreach (var e in entities)
            {
                int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);
                bool hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(e.ApplicationID);
                dtoList.Add(MapToDto(e, count, hasLicense));
            }

            return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<Result<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationByIdAsync(int id)
        {
            var e = await _repository.GetByIdAsync(id);

            if (e == null)
                return Result<LocalDrivingLicenseApplicationListDto>.Fail("طلب الرخصة المحلي غير موجود.");

            int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);
            bool hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(e.ApplicationID);

            return Result<LocalDrivingLicenseApplicationListDto>.Success(MapToDto(e, count, hasLicense));
        }

        // =========================
        // ADD
        // =========================
        public async Task<Result<int>> AddLocalDrivingLicenseApplicationAsync(LocalDrivingLicenseApplicationCreateUpdateDto dto)
        {
            if (dto == null)
                return Result<int>.Fail("بيانات الطلب مطلوبة.");

            var entity = new LocalDrivingLicenseApplication
            {
                ApplicationID = dto.ApplicatonId,
                LicenseClassID = dto.LicenseClassId,
            };

            int id = await _repository.CreateLocalDrivingLicenseApplicationAsync(entity);

            return Result<int>.Success(id);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<Result> UpdateLocalDrivingLicenseApplicationAsync(int id, LocalDrivingLicenseApplicationCreateUpdateDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات الطلب مطلوبة.");

            var existing = await _repository.GetByIdAsync(id);

            if (existing == null)
                return Result.Failure("طلب الرخصة المحلي غير موجود.");

            existing.LicenseClassID = dto.LicenseClassId;

            var isSuccess = await _repository.UpdateAsync(existing);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث الطلب.");
        }

        // =========================
        // DELETE
        // =========================
        public async Task<Result> DeleteLocalDrivingLicenseApplicationAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);

            if (existing == null)
                return Result.Failure("طلب الرخصة المحلي غير موجود.");

            var isSuccess = await _repository.DeleteAsync(id);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في حذف الطلب.");
        }

        // =========================
        // GET BY PERSON ID
        // =========================
        public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId)
        {
            var list = await _repository.GetByPersonIdAsync(applicantPersonId);
            var dtoList = await MapListToDtoAsync(list);

            return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
        }

        // =========================
        // GET BY APPLICATION ID
        // =========================
        public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId)
        {
            var list = await _repository.GetByApplicationIdAsync(applicationId);
            var dtoList = await MapListToDtoAsync(list);

            return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
        }

        // =========================
        // GET BY LICENSE CLASS ID
        // =========================
        public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId)
        {
            var list = await _repository.GetByLicenseClassIdAsync(licenseClassId);
            var dtoList = await MapListToDtoAsync(list);

            return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
        }

        // =========================
        // GET APPLICATION ID BY LOCAL ID
        // =========================
        public async Task<Result<int>> GetApplicationIdByLocalIdAsync(int localId)
        {
            var applicationId = await _repository.GetApplicationIdByLocalIdAsync(localId);

            if (!applicationId.HasValue)
                return Result<int>.Fail("الطلب الرئيسي غير موجود لهذا الطلب المحلي.");

            return Result<int>.Success(applicationId.Value);
        }

        // =========================
        // CHECKS
        // =========================
        public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id)
        {
            return await _repository.GetByIdAsync(id) != null;
        }

        // =========================
        // PRIVATE HELPERS
        // =========================
        private async Task<List<LocalDrivingLicenseApplicationListDto>> MapListToDtoAsync(
            List<LocalDrivingLicenseApplication> list)
        {
            var dtoList = new List<LocalDrivingLicenseApplicationListDto>();

            foreach (var e in list)
            {
                int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);
                bool hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(e.ApplicationID);
                dtoList.Add(MapToDto(e, count, hasLicense));
            }

            return dtoList;
        }

        private LocalDrivingLicenseApplicationListDto MapToDto(
            LocalDrivingLicenseApplication e,
            int passedTestCount,
            bool hasLicense)
        {
            return new LocalDrivingLicenseApplicationListDto
            {
                LocalDrivingLicenseApplicationID = e.LocalDrivingLicenseApplicationID,
                LicenseClassID = e.LicenseClassID,
                LicenseClassName = e.LicenseClass?.ClassName ?? "N/A",
                NationalNo = e.Application?.Person?.NationalNo ?? "N/A",
                Fees = e.LicenseClass?.ClassFees ?? 0,
                FullName =
                    $"{e.Application?.Person?.FirstName} " +
                    $"{e.Application?.Person?.SecondName} " +
                    $"{e.Application?.Person?.ThirdName} " +
                    $"{e.Application?.Person?.LastName}".Trim(),
                ApplicationDate = e.Application?.ApplicationDate ?? DateTime.MinValue,
                PassedTest = passedTestCount,
                ApplicationStatus = e.Application is not null && Enum.IsDefined(typeof(AppStatus), e.Application.ApplicationStatus)
                    ? (AppStatus)e.Application.ApplicationStatus
                    : AppStatus.Cancelled,
                HasLicense = hasLicense,
                ApplicantPersonID = e.Application?.Person?.PersonId ?? 0
            };
        }
    }
}