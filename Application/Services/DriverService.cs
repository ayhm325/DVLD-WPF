using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _repository;

        public DriverService(IDriverRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        // =========================
        // GET
        // =========================

        public async Task<Result<DriverDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null)
                return Result<DriverDto>.Fail("Driver not found.");

            return Result<DriverDto>.Success(MapToDto(entity));
        }

        public async Task<Result<List<DriverDto>>> GetAllAsync()
        {
            var drivers = await _repository.GetAllAsync();

            return Result<List<DriverDto>>.Success(
                drivers.Select(MapToDto).ToList());
        }

        public async Task<Result<DriverDto>> GetByPersonIdAsync(int personId)
        {
            var entity = await _repository.GetByPersonIdAsync(personId);

            if (entity is null)
                return Result<DriverDto>.Fail("Driver not found.");

            return Result<DriverDto>.Success(MapToDto(entity));
        }

        public async Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(int userId)
        {
            var drivers = await _repository.GetByCreatedUserIdAsync(userId);

            return Result<List<DriverDto>>.Success(
                drivers.Select(MapToDto).ToList());
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

        public async Task<Result<int>> AddAsync(DriverDto dto)
        {
            if (dto is null)
                return Result<int>.Fail("Driver data is required.");

            if (await ExistsByPersonIdAsync(dto.PersonID))
                return Result<int>.Fail(
                    "This person is already registered as a driver.");

            var entity = MapToEntity(dto);

            await _repository.AddAsync(entity);

            return Result<int>.Success(entity.DriverID);
        }

        public async Task<Result> UpdateAsync(DriverDto dto)
        {
            if (dto is null)
                return Result.Failure("Driver data is required.");

            if (!await ExistsByIdAsync(dto.DriverID))
                return Result.Failure("Driver not found.");

            var entity = MapToEntity(dto);

            await _repository.UpdateAsync(entity);

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(int id)
        {
            if (!await ExistsByIdAsync(id))
                return Result.Failure("Driver not found.");

            await _repository.DeleteAsync(id);

            return Result.Success();
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