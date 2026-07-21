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
            var entity = await _repository.GetByIdAsync(id);

            return entity == null
                ? null
                : MapToDto(entity);
        }



        public async Task<DetainedLicenseDto?>
            GetActiveDetainByLicenseIdAsync(int licenseId)
        {
            var entity =await _repository.GetActiveDetainByLicenseIdAsync(licenseId);

            return entity == null
                ? null
                : MapToDto(entity);
        }

        public async Task<Result<DetainedLicenseDto>> AddAsync(DetainedLicenseDto dto)
        {

            if (dto.LicenseID <= 0)
            {
                return Result<DetainedLicenseDto>.Fail("Invalid license id.");
            }

            bool exists = await _repository.IsLicenseDetainedAsync(dto.LicenseID);


            if (exists)
            {
                return Result<DetainedLicenseDto>.Fail("License already detained.");
            }

            var entity = new DetainedLicense
            {
                LicenseID = dto.LicenseID,

                DetainDate = dto.DetainDate,

                FineFees = dto.FineFees,

                CreatedByUserID = dto.CreatedByUserID
            };

            var created = await _repository.AddAsync(entity);

            var result = await GetByIdAsync(created.DetainID);

            if (result == null)
            {
                return Result<DetainedLicenseDto>.Fail("Unable to create detained license.");
            }

            return Result<DetainedLicenseDto>.Success(result);
        }





        public async Task<Result> UpdateAsync(
            DetainedLicenseDto dto)
        {

            var entity = await _repository.GetByIdAsync(dto.DetainID);

            if (entity == null)
            {
                return Result.Failure(
                    "Detained license not found.");
            }



            entity.FineFees = dto.FineFees;

            entity.IsReleased = dto.IsReleased;

            entity.ReleaseDate = dto.ReleaseDate;

            entity.ReleasedByUserID =
                dto.ReleasedByUserID;

            entity.ReleaseApplicationID =
                dto.ReleaseApplicationID;



            await _repository.UpdateAsync(entity);



            return Result.Success();
        }





        public async Task<bool> IsLicenseDetainedAsync(
            int licenseId)
        {
            return await _repository
                .IsLicenseDetainedAsync(licenseId);
        }





        public async Task<Result> ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int applicationId)
        {

            var entity =
                await _repository.GetByIdAsync(detainId);



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

            entity.ReleasedByUserID =
                releasedByUserId;

            entity.ReleaseApplicationID =
                applicationId;



            await _repository.UpdateAsync(entity);



            return Result.Success();
        }





        private static DetainedLicenseDto MapToDto(
            DetainedLicense d)
        {
            return new DetainedLicenseDto
            {
                DetainID = d.DetainID,

                LicenseID = d.LicenseID,

                PersonID =
                    d.License?.Driver?.Person?.PersonId ?? 0,


                DetainDate = d.DetainDate,


                FineFees = d.FineFees,


                CreatedByUserID =
                    d.CreatedByUserID,


                CreatedByUserName =
                    d.CreatedByUser?.UserName
                    ?? string.Empty,


                IsReleased = d.IsReleased,


                ReleaseDate = d.ReleaseDate,


                ReleasedByUserID =
                    d.ReleasedByUserID,


                ReleaseApplicationID =
                    d.ReleaseApplicationID,


                NationalNo =
                    d.License?.Driver?.Person?.NationalNo
                    ?? string.Empty,


                FullName =
                    d.License?.Driver?.Person?.FullName
                    ?? string.Empty
            };
        }
    }
}