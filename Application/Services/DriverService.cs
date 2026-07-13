using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class DriverService : IDriverService
    {
        private readonly DriverRepository _repository;
        

        public DriverService(DriverRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
           
        }

        // =========================
        // GET
        // =========================

        public async Task<DriverDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);

            return entity is null
                ? null
                : MapToDto(entity);
        }

        public async Task<List<DriverDto>> GetAllAsync()
        {
            var drivers = await _repository.GetAllAsync();
          
            return drivers
                .Select(MapToDto)
                .ToList();
        }

        public async Task<DriverDto?> GetByPersonIdAsync(int personId)
        {
            var entity = await _repository.GetByPersonIdAsync(personId);

            return entity is null
                ? null
                : MapToDto(entity);
        }

        public async Task<List<DriverDto>> GetByCreatedUserIdAsync(int userId)
        {
            var drivers = await _repository.GetByCreatedUserIdAsync(userId);

            return drivers
                .Select(MapToDto)
                .ToList();
        }

        // =========================
        // CHECKS
        // =========================

        public Task<bool> ExistsByIdAsync(int driverId)
        {
            return _repository.ExistsByIdAsync(driverId);
        }

        public Task<bool> ExistsByPersonIdAsync(int personId)
        {
            return _repository.ExistsByPersonIdAsync(personId);
        }

        // =========================
        // COMMANDS
        // =========================

        public async Task<int> AddAsync(DriverDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (await ExistsByPersonIdAsync(dto.PersonID))
                throw new InvalidOperationException(
                    "This person is already registered as a driver.");

            var entity = MapToEntity(dto);

            await _repository.AddAsync(entity);

            return entity.DriverID;
        }

        public async Task UpdateAsync(DriverDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (!await ExistsByIdAsync(dto.DriverID))
                throw new InvalidOperationException(
                    "Driver not found.");

            var entity = MapToEntity(dto);

            await _repository.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await ExistsByIdAsync(id))
                throw new InvalidOperationException(
                    "Driver not found.");

            await _repository.DeleteAsync(id);
        }

        // =========================
        // MAPPING
        // =========================

        private static DriverDto MapToDto(Driver entity)
        {
            return new DriverDto
            {
                DriverID = entity.DriverID,

                PersonID = entity.PersonID,

                FullName = entity.Person?.FullName ?? string.Empty,

                NationalNo = entity.Person?.NationalNo ?? string.Empty,

                DateOfBirth = entity.Person?.DateOfBirth ?? DateTime.MinValue,                

                CreatedByUserID = entity.CreatedByUserID,

                CreatedByUserName = entity.CreatedByUser?.UserName ?? string.Empty,

                CreatedDate = entity.CreatedDate,

                ActiveLicenses = entity.Licenses?.Count(l => l.IsActive) ?? 0
            };
        }

        private static Driver MapToEntity(DriverDto dto)
        {
            return new Driver
            {
                DriverID = dto.DriverID,

                PersonID = dto.PersonID,

                CreatedByUserID = dto.CreatedByUserID,

                CreatedDate = dto.CreatedDate
            };
        }
    }
}