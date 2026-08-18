using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class DetainedLicenseService : IDetainedLicenseService
    {
        private readonly IDetainedLicenseRepository _repository;

        public DetainedLicenseService(
            IDetainedLicenseRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DetainedLicenseDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();

            return entities
                .Select(MapToDto)
                .ToList();
        }

        public async Task<DetainedLicenseDto?> GetByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            var entity = await _repository.GetByIdAsync(id);

            return entity == null
                ? null
                : MapToDto(entity);
        }

        public async Task<DetainedLicenseDto?>
            GetActiveDetainByLicenseIdAsync(int licenseId)
        {
            if (licenseId <= 0)
                return null;

            var entity =
                await _repository.GetActiveDetainByLicenseIdAsync(
                    licenseId);

            return entity == null
                ? null
                : MapToDto(entity);
        }

        public async Task<Result<DetainedLicenseDto>> AddAsync(
            DetainedLicenseDto dto)
        {
            if (dto.LicenseID <= 0)
            {
                return Result<DetainedLicenseDto>.Fail(
                    "Invalid license id.");
            }

            if (dto.FineFees < 0)
            {
                return Result<DetainedLicenseDto>.Fail(
                    "Fine fees cannot be negative.");
            }

            bool exists =
                await _repository.IsLicenseDetainedAsync(
                    dto.LicenseID);

            if (exists)
            {
                return Result<DetainedLicenseDto>.Fail(
                    "License already detained.");
            }

            var entity = new DetainedLicense
            {
                LicenseID = dto.LicenseID,
                DetainDate = dto.DetainDate,
                FineFees = dto.FineFees,
                CreatedByUserID = dto.CreatedByUserID
            };

            var created =
                await _repository.AddAsync(entity);

            var result =
                await GetByIdAsync(created.DetainID);

            if (result == null)
            {
                return Result<DetainedLicenseDto>.Fail(
                    "Unable to create detained license.");
            }

            return Result<DetainedLicenseDto>.Success(result);
        }

        public async Task<Result> UpdateAsync(
            DetainedLicenseDto dto)
        {
            if (dto.DetainID <= 0)
            {
                return Result.Failure(
                    "Invalid detained license id.");
            }

            if (dto.FineFees < 0)
            {
                return Result.Failure(
                    "Fine fees cannot be negative.");
            }

            var entity =
                await _repository.GetByIdAsync(
                    dto.DetainID);

            if (entity == null)
            {
                return Result.Failure(
                    "Detained license not found.");
            }

            entity.FineFees = dto.FineFees;
            entity.IsReleased = dto.IsReleased;
            entity.ReleaseDate = dto.ReleaseDate;
            entity.ReleasedByUserID = dto.ReleasedByUserID;
            entity.ReleaseApplicationID = dto.ReleaseApplicationID;

            await _repository.UpdateAsync(entity);

            return Result.Success();
        }

        public async Task<bool> IsLicenseDetainedAsync(
            int licenseId)
        {
            if (licenseId <= 0)
                return false;

            return await _repository
                .IsLicenseDetainedAsync(licenseId);
        }

        public async Task<Result> ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int applicationId)
        {
            if (detainId <= 0)
            {
                return Result.Failure(
                    "Invalid detained license id.");
            }

            if (releasedByUserId <= 0)
            {
                return Result.Failure(
                    "Invalid releasing user id.");
            }

            if (applicationId <= 0)
            {
                return Result.Failure(
                    "Invalid release application id.");
            }

            var entity =
                await _repository.GetByIdAsync(
                    detainId);

            if (entity == null)
            {
                return Result.Failure(
                    "Detained license not found.");
            }

            if (entity.IsReleased)
            {
                return Result.Failure(
                    "License already released.");
            }

            entity.IsReleased = true;
            entity.ReleaseDate = DateTime.Now;
            entity.ReleasedByUserID = releasedByUserId;
            entity.ReleaseApplicationID = applicationId;

            await _repository.UpdateAsync(entity);

            return Result.Success();
        }

        private static DetainedLicenseDto MapToDto(
            DetainedLicense d)
        {
            var person =
                d.License?.Driver?.Person;

            return new DetainedLicenseDto
            {
                DetainID = d.DetainID,
                LicenseID = d.LicenseID,

                PersonID =
                    person?.PersonId ?? 0,

                ApplicantPersonID =
                    person?.PersonId ?? 0,

                DetainDate =
                    d.DetainDate,

                FineFees =
                    d.FineFees,

                CreatedByUserID =
                    d.CreatedByUserID,

                CreatedByUserName =
                    d.CreatedByUser?.UserName
                    ?? string.Empty,

                IsReleased =
                    d.IsReleased,

                ReleaseDate =
                    d.ReleaseDate,

                ReleasedByUserID =
                    d.ReleasedByUserID,

                ReleaseApplicationID =
                    d.ReleaseApplicationID,

                NationalNo =
                    person?.NationalNo
                    ?? string.Empty,

                FullName =
                    person?.FullName
                    ?? string.Empty
            };
        }
    }
}