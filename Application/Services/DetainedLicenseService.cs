using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class DetainedLicenseService : IDetainedLicenseService
    {
        private readonly DetainedLicenseRepository _repository;

        public DetainedLicenseService(DetainedLicenseRepository repository)
        {
            _repository = repository;
        }

        public async Task<DetainedLicense?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public async Task<List<DetainedLicense>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<DetainedLicense> AddAsync(DetainedLicense entity) => await _repository.AddAsync(entity);

        public async Task UpdateAsync(DetainedLicense entity) => await _repository.UpdateAsync(entity);

        public async Task<bool> IsLicenseDetainedAsync(int licenseId) => await _repository.IsLicenseDetainedAsync(licenseId);

        public async Task ReleaseAsync(int detainId, int releasedByUserId, int applicationId)
            => await _repository.ReleaseAsync(detainId, releasedByUserId, applicationId);
    }
}