using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class DetainedLicenseService : IDetainedLicenseService
    {
        private readonly DetainedLicenseRepository _repository;


        public DetainedLicenseService(
            DetainedLicenseRepository repository)
        {
            _repository = repository;
        }


        public async Task<List<DetainedLicenseDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();


            return list.Select(d => new DetainedLicenseDto
            {
                DetainID = d.DetainID,

                LicenseID = d.LicenseID,

                DetainDate = d.DetainDate,

                FineFees = d.FineFees,

                CreatedByUserID = d.CreatedByUserID,

                IsReleased = d.IsReleased,

                ReleaseDate = d.ReleaseDate,

                ReleasedByUserID = d.ReleasedByUserID,

                ReleaseApplicationID = d.ReleaseApplicationID,

                ApplicantPersonID = d.License.Driver.Person.PersonId,

                NationalNo = d.License.Driver.Person.NationalNo,

                FullName =
                    $"{d.License.Driver.Person.FirstName} " +
                    $"{d.License.Driver.Person.SecondName} " +
                    $"{d.License.Driver.Person.ThirdName} " +
                    $"{d.License.Driver.Person.LastName}"

            }).ToList();
        }



        public async Task<DetainedLicenseDto?> GetByIdAsync(int id)
        {
            var d = await _repository.GetByIdAsync(id);

            if (d == null)
                return null;


            return new DetainedLicenseDto
            {
                DetainID = d.DetainID,

                LicenseID = d.LicenseID,

                DetainDate = d.DetainDate,

                FineFees = d.FineFees,

                CreatedByUserID = d.CreatedByUserID,

                IsReleased = d.IsReleased,

                ReleaseDate = d.ReleaseDate,

                ReleasedByUserID = d.ReleasedByUserID,

                ReleaseApplicationID = d.ReleaseApplicationID,

                ApplicantPersonID = d.License.Driver.Person.PersonId,

                NationalNo = d.License.Driver.Person.NationalNo,

                FullName =
                    $"{d.License.Driver.Person.FirstName} " +
                    $"{d.License.Driver.Person.SecondName} " +
                    $"{d.License.Driver.Person.ThirdName} " +
                    $"{d.License.Driver.Person.LastName}"
            };
        }



        public async Task<DetainedLicense> AddAsync(DetainedLicense entity)
        {
            return await _repository.AddAsync(entity);
        }


        public async Task UpdateAsync(DetainedLicense entity)
        {
            await _repository.UpdateAsync(entity);
        }


        public async Task<bool> IsLicenseDetainedAsync(int licenseId)
        {
            return await _repository.IsLicenseDetainedAsync(licenseId);
        }


        public async Task ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int applicationId)
        {
            await _repository.ReleaseAsync(
                detainId,
                releasedByUserId,
                applicationId);
        }
    }
}